using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsfsFailures.Data.Repositories;
using MsfsFailures.Sim;

namespace MsfsFailures.App.Services;

/// <summary>
/// Resolves the active airframe by trying N208RC first, then falling back to the first airframe in the DB.
/// Subscribes to ISimBus.StatusStream to refresh resolution when sim connects/disconnects.
/// Raises ActiveAirframeChanged event when the resolved ID changes.
/// </summary>
public sealed class ActiveAirframeProvider : IActiveAirframeProvider, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISimBus _bus;
    private readonly ILogger<ActiveAirframeProvider> _log;
    private readonly object _lock = new();
    private Guid? _cachedAirframeId;
    private IDisposable? _statusSub;

    public event EventHandler<Guid?>? ActiveAirframeChanged;

    public ActiveAirframeProvider(
        IServiceScopeFactory scopeFactory,
        ISimBus bus,
        ILogger<ActiveAirframeProvider> log)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _log = log;

        // Subscribe to sim status changes to re-resolve airframe.
        _statusSub = _bus.StatusStream.Subscribe(
            status => OnSimStatusChanged(status),
            ex => _log.LogError(ex, "ActiveAirframeProvider: StatusStream error."));
    }

    public async Task<Guid?> GetActiveAirframeIdAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_cachedAirframeId.HasValue)
                return _cachedAirframeId;
        }

        var resolved = await ResolveAirframeIdAsync(ct);

        lock (_lock)
        {
            if (_cachedAirframeId != resolved)
            {
                _cachedAirframeId = resolved;
                _log.LogInformation("ActiveAirframeProvider: airframe resolved to {AirframeId}.", resolved);
                ActiveAirframeChanged?.Invoke(this, resolved);
            }
        }

        return resolved;
    }

    private void OnSimStatusChanged(SimStatus status)
    {
        // Re-resolve airframe when sim connects or disconnects.
        if (status.State == SimConnectionState.Connected || status.State == SimConnectionState.Offline)
        {
            // Fire and forget — clear cache and let next caller resolve.
            lock (_lock)
            {
                _cachedAirframeId = null;
            }
        }
    }

    private async Task<Guid?> ResolveAirframeIdAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();

            // Try N208RC first (seeded demo aircraft).
            var byTail = await repo.GetAirframeByTailAsync("N208RC", ct);
            if (byTail is not null)
                return byTail.Id;

            // Fall back to first airframe in DB.
            var all = await repo.GetAllAirframesAsync(ct);
            if (all.Count > 0)
                return all[0].Id;

            _log.LogInformation("ActiveAirframeProvider: no airframes found in DB.");
            return null;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ActiveAirframeProvider: failed to resolve airframe.");
            return null;
        }
    }

    public void Dispose()
    {
        _statusSub?.Dispose();
    }
}

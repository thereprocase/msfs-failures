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
        // This subscription triggers on every status update, including aircraft identity changes,
        // which are merged into SimStatus by SimBus.
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
        // Re-resolve airframe when status changes (including aircraft identity changes).
        // Fire and forget — clear cache and let next caller resolve.
        lock (_lock)
        {
            _cachedAirframeId = null;
        }
    }

    private async Task<Guid?> ResolveAirframeIdAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();

            // Priority 1: Match aircraft title against ModelRef sim match rules.
            var simStatus = _bus.CurrentStatus;
            if (!string.IsNullOrWhiteSpace(simStatus?.AircraftTitle))
            {
                _log.LogInformation("ActiveAirframeProvider: resolving by aircraft title '{AircraftTitle}'.", simStatus.AircraftTitle);

                var modelRefs = await repo.GetAllModelRefsAsync(ct);
                foreach (var modelRef in modelRefs)
                {
                    if (SimMatchRules.Matches(modelRef.SimMatchRulesJson, simStatus.AircraftTitle, simStatus.AtcModel ?? ""))
                    {
                        _log.LogInformation("ActiveAirframeProvider: matched ModelRef '{ModelRefName}' (ID: {ModelRefId}).", modelRef.Name, modelRef.Id);

                        var airframe = await repo.GetAirframeByModelRefAsync(modelRef.Id, ct);
                        if (airframe is not null)
                        {
                            _log.LogInformation("ActiveAirframeProvider: resolved to airframe '{Tail}' (ID: {AirframeId}).", airframe.Tail, airframe.Id);
                            return airframe.Id;
                        }
                    }
                }

                _log.LogInformation("ActiveAirframeProvider: no matching ModelRef found for title '{AircraftTitle}'.", simStatus.AircraftTitle);
            }

            // Priority 2: Try N208RC (seeded demo aircraft).
            var byTail = await repo.GetAirframeByTailAsync("N208RC", ct);
            if (byTail is not null)
            {
                _log.LogInformation("ActiveAirframeProvider: resolved to seeded demo tail N208RC (ID: {AirframeId}).", byTail.Id);
                return byTail.Id;
            }

            // Priority 3: Fall back to first airframe in DB.
            var all = await repo.GetAllAirframesAsync(ct);
            if (all.Count > 0)
            {
                _log.LogInformation("ActiveAirframeProvider: resolved to first airframe in DB: '{Tail}' (ID: {AirframeId}).", all[0].Tail, all[0].Id);
                return all[0].Id;
            }

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

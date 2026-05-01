using Microsoft.Extensions.Logging;
using MsfsFailures.Core.Wear;

namespace MsfsFailures.Sim.Internal;

/// <summary>
/// <see cref="ISimConnectClient"/> that wraps both a <see cref="RealSimConnectClient"/> and a
/// <see cref="MockSimConnectClient"/>.
///
/// <para>On <see cref="OpenAsync"/>:
/// <list type="number">
///   <item>Tries the real client first.</item>
///   <item>If it throws (MSFS not running, COM error, etc.) logs a warning and transparently
///       falls through to the mock client.</item>
/// </list>
/// </para>
///
/// <para>This is the default registration when <see cref="SimMode.Auto"/> is selected (or when
/// no mode is specified), so the app runs in dev/CI even without MSFS installed.</para>
/// </summary>
internal sealed class FallbackSimConnectClient : ISimConnectClient
{
    private readonly RealSimConnectClient _real;
    private readonly MockSimConnectClient _mock;
    private readonly ILogger<FallbackSimConnectClient> _logger;

    private ISimConnectClient? _active;

    public FallbackSimConnectClient(
        RealSimConnectClient real,
        MockSimConnectClient mock,
        ILogger<FallbackSimConnectClient> logger)
    {
        _real   = real;
        _mock   = mock;
        _logger = logger;

        // Forward events from whichever inner client becomes active.
        // We wire them to local re-raise delegates below.
        _real.Connected              += OnInnerConnected;
        _real.Disconnected           += OnInnerDisconnected;
        _real.Error                  += OnInnerError;
        _real.AircraftIdentityReceived += OnInnerAircraftIdentity;
        _real.SampleProduced         += OnInnerSampleProduced;

        _mock.Connected              += OnInnerConnected;
        _mock.Disconnected           += OnInnerDisconnected;
        _mock.Error                  += OnInnerError;
        _mock.AircraftIdentityReceived += OnInnerAircraftIdentity;
        _mock.SampleProduced         += OnInnerSampleProduced;
    }

    // ── ISimConnectClient events ─────────────────────────────────────────────

    public event EventHandler<SimConnectedEventArgs>?     Connected;
    public event EventHandler?                            Disconnected;
    public event EventHandler<SimErrorEventArgs>?         Error;
    public event EventHandler<AircraftIdentityEventArgs>? AircraftIdentityReceived;
    public event EventHandler<FlightSampleEventArgs>?     SampleProduced;

    // ── ISimConnectClient API ────────────────────────────────────────────────

    public async Task OpenAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("FallbackSimConnectClient: trying RealSimConnectClient…");
            // Set _active before awaiting so that OnRecvOpen (which fires Connected) is forwarded.
            _active = _real;
            await _real.OpenAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("FallbackSimConnectClient: connected to MSFS via SimConnect.");
        }
        catch (OperationCanceledException)
        {
            _active = null;
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "FallbackSimConnectClient: RealSimConnectClient failed ({Message}); falling back to mock.",
                ex.Message);

            // Set _active before awaiting mock.OpenAsync so that the Connected event
            // (fired inside OpenAsync by MockSimConnectClient) is forwarded correctly.
            _active = _mock;
            await _mock.OpenAsync(ct).ConfigureAwait(false);
        }
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        return _active?.CloseAsync(ct) ?? Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _real.Connected              -= OnInnerConnected;
        _real.Disconnected           -= OnInnerDisconnected;
        _real.Error                  -= OnInnerError;
        _real.AircraftIdentityReceived -= OnInnerAircraftIdentity;
        _real.SampleProduced         -= OnInnerSampleProduced;

        _mock.Connected              -= OnInnerConnected;
        _mock.Disconnected           -= OnInnerDisconnected;
        _mock.Error                  -= OnInnerError;
        _mock.AircraftIdentityReceived -= OnInnerAircraftIdentity;
        _mock.SampleProduced         -= OnInnerSampleProduced;

        await _real.DisposeAsync().ConfigureAwait(false);
        await _mock.DisposeAsync().ConfigureAwait(false);
    }

    // ── Inner-client event forwarders ────────────────────────────────────────
    // Only forward events from whichever inner client is currently active.

    private void OnInnerConnected(object? sender, SimConnectedEventArgs e)
    {
        if (sender == _active) Connected?.Invoke(this, e);
    }

    private void OnInnerDisconnected(object? sender, EventArgs e)
    {
        if (sender == _active) Disconnected?.Invoke(this, e);
    }

    private void OnInnerError(object? sender, SimErrorEventArgs e)
    {
        if (sender == _active) Error?.Invoke(this, e);
    }

    private void OnInnerAircraftIdentity(object? sender, AircraftIdentityEventArgs e)
    {
        if (sender == _active) AircraftIdentityReceived?.Invoke(this, e);
    }

    private void OnInnerSampleProduced(object? sender, FlightSampleEventArgs e)
    {
        if (sender == _active) SampleProduced?.Invoke(this, e);
    }
}

using Microsoft.Extensions.Logging;
using MsfsFailures.Core.Wear;

namespace MsfsFailures.Sim.Internal;

/// <summary>
/// No-op implementation of <see cref="ISimConnectClient"/> used when the MSFS SDK is not available.
/// Simulates a 300 ms connect delay so the UI state machine feels realistic in dev builds.
/// While connected it drives a synthetic Cessna 208B Caravan flight profile (see
/// <see cref="SyntheticFlightProfile"/>) that loops continuously through all flight phases.
///
/// <para>
/// Replace this registration with a <c>RealSimConnectClient</c> at integration time by
/// overriding the DI registration after calling <see cref="SimServiceCollectionExtensions.AddMsfsFailuresSim"/>.
/// See <see cref="ISimConnectClient"/> for the swap-point documentation.
/// </para>
/// </summary>
internal sealed class MockSimConnectClient : ISimConnectClient
{
    /// <summary>Sample interval in milliseconds (250 ms = 4 Hz).</summary>
    private const int SampleIntervalMs = 250;

    /// <summary>Log a sample line every N ticks (40 × 250 ms = 10 s).</summary>
    private const int LogEveryNTicks = 40;

    private readonly ILogger<MockSimConnectClient> _logger;
    private readonly int _randomSeed;

    private bool _connected;
    private Timer? _sampleTimer;
    private SyntheticFlightProfile? _profile;
    private int _tickCount;

    public MockSimConnectClient(ILogger<MockSimConnectClient> logger, int randomSeed = 42)
    {
        _logger     = logger;
        _randomSeed = randomSeed;
    }

    public event EventHandler<SimConnectedEventArgs>? Connected;
    public event EventHandler? Disconnected;
#pragma warning disable CS0067 // Event is never used — required by ISimConnectClient; real client raises this on sim exceptions
    public event EventHandler<SimErrorEventArgs>? Error;
#pragma warning restore CS0067
    public event EventHandler<AircraftIdentityEventArgs>? AircraftIdentityReceived;
    public event EventHandler<FlightSampleEventArgs>? SampleProduced;

    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (_connected)
            throw new InvalidOperationException("MockSimConnectClient is already connected.");

        _logger.LogInformation("[Mock] Simulating SimConnect connect delay (300 ms)…");
        await Task.Delay(300, ct).ConfigureAwait(false);

        _connected  = true;
        _tickCount  = 0;
        _profile    = new SyntheticFlightProfile(new Random(_randomSeed));

        _logger.LogInformation("[Mock] SimConnect session opened (mock). Starting synthetic flight profile (cycle ≈ {Sec:F0} s).",
            SyntheticFlightProfile.NominalCycleDurationSec);

        Connected?.Invoke(this, new SimConnectedEventArgs { SimVersion = "MockSim/1.0" });

        AircraftIdentityReceived?.Invoke(this, new AircraftIdentityEventArgs
        {
            AircraftTitle = "Mock Aircraft – Cessna 208B Caravan (no MSFS SDK)",
            AtcModel      = "C208",
        });

        // Start the sample pump at 4 Hz.
        _sampleTimer = new Timer(OnSampleTick, null, SampleIntervalMs, SampleIntervalMs);
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        if (!_connected)
            return Task.CompletedTask;

        StopSampleTimer();
        _connected = false;
        _logger.LogInformation("[Mock] SimConnect session closed (mock).");
        Disconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_connected)
        {
            StopSampleTimer();
            _connected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        return ValueTask.CompletedTask;
    }

    // ── Sample pump ──────────────────────────────────────────────────────────

    private void OnSampleTick(object? _)
    {
        if (!_connected || _profile is null) return;

        const double elapsedSec = SampleIntervalMs / 1000.0;
        var sample = _profile.Tick(elapsedSec);

        SampleProduced?.Invoke(this, new FlightSampleEventArgs { Sample = sample });

        _tickCount++;
        if (_tickCount % LogEveryNTicks == 0)
        {
            _logger.LogInformation(
                "[Mock] sample tick · phase={Phase} ias={Ias:F0} kt  itt={Itt:F0} °C",
                _profile.CurrentPhase, sample.IasKt, sample.IttC);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void StopSampleTimer()
    {
        _sampleTimer?.Dispose();
        _sampleTimer = null;
    }
}

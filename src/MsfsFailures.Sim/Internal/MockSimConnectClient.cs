using Microsoft.Extensions.Logging;

namespace MsfsFailures.Sim.Internal;

/// <summary>
/// No-op implementation of <see cref="ISimConnectClient"/> used when the MSFS SDK is not available.
/// Simulates a 300 ms connect delay so the UI state machine feels realistic in dev builds.
///
/// <para>
/// Replace this registration with a <c>RealSimConnectClient</c> at integration time by
/// overriding the DI registration after calling <see cref="SimServiceCollectionExtensions.AddMsfsFailuresSim"/>.
/// See <see cref="ISimConnectClient"/> for the swap-point documentation.
/// </para>
/// </summary>
internal sealed class MockSimConnectClient : ISimConnectClient
{
    private readonly ILogger<MockSimConnectClient> _logger;
    private bool _connected;

    public MockSimConnectClient(ILogger<MockSimConnectClient> logger)
    {
        _logger = logger;
    }

    public event EventHandler<SimConnectedEventArgs>? Connected;
    public event EventHandler? Disconnected;
#pragma warning disable CS0067 // Event is never used — required by ISimConnectClient; real client raises this on sim exceptions
    public event EventHandler<SimErrorEventArgs>? Error;
#pragma warning restore CS0067
    public event EventHandler<AircraftIdentityEventArgs>? AircraftIdentityReceived;

    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (_connected)
            throw new InvalidOperationException("MockSimConnectClient is already connected.");

        _logger.LogInformation("[Mock] Simulating SimConnect connect delay (300 ms)…");
        await Task.Delay(300, ct).ConfigureAwait(false);

        _connected = true;
        _logger.LogInformation("[Mock] SimConnect session opened (mock).");
        Connected?.Invoke(this, new SimConnectedEventArgs { SimVersion = "MockSim/1.0" });

        // Emit a synthetic aircraft-identity update so consumers see something plausible.
        AircraftIdentityReceived?.Invoke(this, new AircraftIdentityEventArgs
        {
            AircraftTitle = "Mock Aircraft (no MSFS SDK)",
            AtcModel = "MOCK",
        });
    }

    public Task CloseAsync(CancellationToken ct = default)
    {
        if (!_connected)
            return Task.CompletedTask;

        _connected = false;
        _logger.LogInformation("[Mock] SimConnect session closed (mock).");
        Disconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_connected)
        {
            _connected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
        return ValueTask.CompletedTask;
    }
}

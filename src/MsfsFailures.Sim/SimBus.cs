using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using MsfsFailures.Sim.Internal;

namespace MsfsFailures.Sim;

/// <summary>
/// Default implementation of <see cref="ISimBus"/>.
/// Wraps an <see cref="ISimConnectClient"/>, manages connection lifecycle, and broadcasts
/// <see cref="SimStatus"/> snapshots via a <see cref="BehaviorSubject{T}"/>.
/// </summary>
internal sealed class SimBus : ISimBus, IAsyncDisposable
{
    private readonly ILogger<SimBus> _logger;
    private readonly ISimConnectClient _client;
    private readonly BehaviorSubject<SimStatus> _subject;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public SimBus(ILogger<SimBus> logger, ISimConnectClient client)
    {
        _logger = logger;
        _client = client;

        // Seed the subject with Offline so new subscribers immediately get a value.
        _subject = new BehaviorSubject<SimStatus>(SimStatus.Offline());

        // Wire client events → status pushes.
        _client.Connected += OnClientConnected;
        _client.Disconnected += OnClientDisconnected;
        _client.Error += OnClientError;
        _client.AircraftIdentityReceived += OnAircraftIdentityReceived;
    }

    // ── ISimBus ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public SimStatus CurrentStatus => _subject.Value;

    /// <inheritdoc/>
    public IObservable<SimStatus> StatusStream => _subject;

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_subject.Value.State is SimConnectionState.Connected or SimConnectionState.Connecting)
            {
                _logger.LogDebug("ConnectAsync called but already {State}; ignoring.", _subject.Value.State);
                return;
            }

            Publish(SimStatus.Connecting());

            try
            {
                await _client.OpenAsync(ct).ConfigureAwait(false);
                // Connected/Error status is emitted by the event handlers below.
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ConnectAsync cancelled.");
                Publish(SimStatus.Offline());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SimConnect connect failed.");
                Publish(SimStatus.Error(ex.Message));
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_subject.Value.State is SimConnectionState.Offline)
            {
                _logger.LogDebug("DisconnectAsync called but already Offline; ignoring.");
                return;
            }

            try
            {
                await _client.CloseAsync(ct).ConfigureAwait(false);
                // Offline status is emitted by the Disconnected event handler below.
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DisconnectAsync cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SimConnect disconnect failed; forcing Offline.");
                Publish(SimStatus.Offline());
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Client event handlers ────────────────────────────────────────────────

    private void OnClientConnected(object? sender, SimConnectedEventArgs e)
    {
        _logger.LogInformation("SimConnect session opened. SimVersion={SimVersion}", e.SimVersion);
        // Aircraft info will arrive shortly via AircraftIdentityReceived.
        Publish(SimStatus.Connected());
    }

    private void OnClientDisconnected(object? sender, EventArgs e)
    {
        _logger.LogInformation("SimConnect session closed.");
        Publish(SimStatus.Offline());
    }

    private void OnClientError(object? sender, SimErrorEventArgs e)
    {
        _logger.LogError(e.Exception, "SimConnect error: {Message}", e.Message);
        Publish(SimStatus.Error(e.Message));
    }

    private void OnAircraftIdentityReceived(object? sender, AircraftIdentityEventArgs e)
    {
        _logger.LogInformation(
            "Aircraft identity: Title={Title} AtcModel={AtcModel}",
            e.AircraftTitle, e.AtcModel);

        // Merge into current status preserving connection state.
        var current = _subject.Value;
        Publish(current with
        {
            AircraftTitle = e.AircraftTitle,
            AtcModel = e.AtcModel,
            Timestamp = DateTimeOffset.UtcNow,
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void Publish(SimStatus status)
    {
        if (!_disposed)
            _subject.OnNext(status);
    }

    // ── IAsyncDisposable ─────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _client.Connected -= OnClientConnected;
        _client.Disconnected -= OnClientDisconnected;
        _client.Error -= OnClientError;
        _client.AircraftIdentityReceived -= OnAircraftIdentityReceived;

        await _client.DisposeAsync().ConfigureAwait(false);

        _subject.OnCompleted();
        _subject.Dispose();
        _lock.Dispose();
    }
}

namespace MsfsFailures.Sim;

/// <summary>
/// Primary abstraction for the SimConnect integration layer.
/// Consumers observe <see cref="StatusStream"/> to react to connection state and aircraft changes.
/// </summary>
public interface ISimBus
{
    /// <summary>The most-recently-emitted status snapshot. Never <c>null</c> — starts as <see cref="SimConnectionState.Offline"/>.</summary>
    SimStatus CurrentStatus { get; }

    /// <summary>
    /// Hot observable that replays the last emitted status to new subscribers, then pushes
    /// subsequent updates. Backed by a <c>BehaviorSubject&lt;SimStatus&gt;</c> from System.Reactive.
    /// Completes when the bus is disposed.
    /// </summary>
    IObservable<SimStatus> StatusStream { get; }

    /// <summary>
    /// Initiates connection to the simulator. Transitions through
    /// <see cref="SimConnectionState.Connecting"/> → <see cref="SimConnectionState.Connected"/>
    /// (or <see cref="SimConnectionState.Error"/> on failure).
    /// Safe to call when already connected — returns immediately in that case.
    /// Never throws; all errors are surfaced via <see cref="StatusStream"/>.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Gracefully closes the SimConnect session and transitions to <see cref="SimConnectionState.Offline"/>.
    /// Safe to call when already offline — returns immediately in that case.
    /// Never throws.
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);
}

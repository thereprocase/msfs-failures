namespace MsfsFailures.Sim;

/// <summary>
/// Immutable snapshot of the SimConnect connection state, broadcast on <see cref="ISimBus.StatusStream"/>.
/// </summary>
/// <param name="State">Current connection lifecycle state.</param>
/// <param name="ErrorMessage">Human-readable error message when <paramref name="State"/> is <see cref="SimConnectionState.Error"/>; otherwise <c>null</c>.</param>
/// <param name="AircraftTitle">Full aircraft title from the <c>Title</c> SimVar (e.g. "Asobo Cessna 172 G1000"); <c>null</c> when not connected or no aircraft loaded.</param>
/// <param name="AtcModel">ICAO ATC model code (e.g. "C172"); <c>null</c> when not available.</param>
/// <param name="Timestamp">Wall-clock time at which this status was produced.</param>
public sealed record SimStatus(
    SimConnectionState State,
    string? ErrorMessage,
    string? AircraftTitle,
    string? AtcModel,
    DateTimeOffset Timestamp)
{
    /// <summary>Convenience factory for an Offline status with no aircraft information.</summary>
    public static SimStatus Offline() =>
        new(SimConnectionState.Offline, null, null, null, DateTimeOffset.UtcNow);

    /// <summary>Convenience factory for a Connecting status.</summary>
    public static SimStatus Connecting() =>
        new(SimConnectionState.Connecting, null, null, null, DateTimeOffset.UtcNow);

    /// <summary>Convenience factory for a Connected status with optional aircraft info.</summary>
    public static SimStatus Connected(string? aircraftTitle = null, string? atcModel = null) =>
        new(SimConnectionState.Connected, null, aircraftTitle, atcModel, DateTimeOffset.UtcNow);

    /// <summary>Convenience factory for an Error status.</summary>
    public static SimStatus Error(string errorMessage) =>
        new(SimConnectionState.Error, errorMessage, null, null, DateTimeOffset.UtcNow);
}

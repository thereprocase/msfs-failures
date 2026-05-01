namespace MsfsFailures.Sim;

/// <summary>
/// Reflects the lifecycle state of the SimConnect session.
/// </summary>
public enum SimConnectionState
{
    /// <summary>Not connected; no attempt in progress.</summary>
    Offline,

    /// <summary>Connection attempt is in progress.</summary>
    Connecting,

    /// <summary>SimConnect session is open and healthy.</summary>
    Connected,

    /// <summary>An unrecoverable error occurred; see <see cref="SimStatus.ErrorMessage"/>.</summary>
    Error,
}

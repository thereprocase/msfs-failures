using MsfsFailures.Core.Wear;

namespace MsfsFailures.Sim.Internal;

/// <summary>
/// Abstraction over the real Microsoft.FlightSimulator.SimConnect managed wrapper.
///
/// <para>
/// <b>Integration-time swap:</b> Replace <see cref="MockSimConnectClient"/> with
/// <c>RealSimConnectClient</c> (to be added in a future pass) that wraps the actual
/// <c>Microsoft.FlightSimulator.SimConnect.SimConnect</c> class obtained from the MSFS SDK:
/// <list type="bullet">
///   <item><c>&lt;SDK&gt;\SimConnect SDK\lib\managed\Microsoft.FlightSimulator.SimConnect.dll</c> — add as a project reference.</item>
///   <item><c>&lt;SDK&gt;\SimConnect SDK\lib\SimConnect.dll</c> — native x64 DLL; set "Copy to Output Directory = Always".</item>
/// </list>
/// The real implementation needs an HWND for the Win32 window-message pump (use
/// <c>WindowInteropHelper.EnsureHandle()</c> + <c>HwndSource.AddHook</c>) or a background timer
/// calling <c>SimConnect.ReceiveMessage()</c> on the UI thread.
/// See <c>/reference/simconnect/simconnect-managed-primer.md</c> for the full pattern.
/// </para>
///
/// <para>
/// <b>MobiFlight WASM note:</b> Do NOT add MobiFlight client-data-area logic to this interface.
/// That belongs in a separate <c>IMobiFlightWasmClient</c> that sits on top of this transport.
/// This interface is purely the SimConnect session lifecycle + aircraft-detection hook point.
/// </para>
/// </summary>
internal interface ISimConnectClient : IAsyncDisposable
{
    /// <summary>Raised when the SimConnect session is successfully opened.</summary>
    event EventHandler<SimConnectedEventArgs> Connected;

    /// <summary>Raised when the SimConnect session is closed by the simulator (e.g. MSFS quit).</summary>
    event EventHandler Disconnected;

    /// <summary>Raised when a SimConnect exception is received.</summary>
    event EventHandler<SimErrorEventArgs> Error;

    /// <summary>
    /// Raised when aircraft identity data arrives (Title + ATC MODEL).
    /// Fires once after connection and again whenever the loaded aircraft changes.
    /// The real implementation subscribes to the "AircraftLoaded" system event and
    /// re-requests the identity data struct.
    /// </summary>
    event EventHandler<AircraftIdentityEventArgs> AircraftIdentityReceived;

    /// <summary>
    /// Raised at ~4 Hz while connected, carrying the latest <see cref="MsfsFailures.Core.Wear.FlightTickSample"/>.
    /// The mock implementation drives this from a synthetic flight-profile state machine.
    /// The real implementation will drive it from SimConnect data-request callbacks.
    /// </summary>
    event EventHandler<FlightSampleEventArgs> SampleProduced;

    /// <summary>
    /// Open the SimConnect connection. Implementations should be non-blocking; the
    /// <see cref="Connected"/> event fires asynchronously when the sim accepts the session.
    /// Throws <see cref="InvalidOperationException"/> if already connected.
    /// </summary>
    Task OpenAsync(CancellationToken ct = default);

    /// <summary>
    /// Close the SimConnect connection gracefully.
    /// Safe to call when not connected.
    /// </summary>
    Task CloseAsync(CancellationToken ct = default);
}

/// <summary>Arguments for the <see cref="ISimConnectClient.Connected"/> event.</summary>
internal sealed class SimConnectedEventArgs : EventArgs
{
    public string SimVersion { get; init; } = string.Empty;
}

/// <summary>Arguments for the <see cref="ISimConnectClient.Error"/> event.</summary>
internal sealed class SimErrorEventArgs : EventArgs
{
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}

/// <summary>Arguments for the <see cref="ISimConnectClient.AircraftIdentityReceived"/> event.</summary>
internal sealed class AircraftIdentityEventArgs : EventArgs
{
    public string? AircraftTitle { get; init; }
    public string? AtcModel { get; init; }
}

/// <summary>Arguments for the <see cref="ISimConnectClient.SampleProduced"/> event.</summary>
internal sealed class FlightSampleEventArgs : EventArgs
{
    public required FlightTickSample Sample { get; init; }
}

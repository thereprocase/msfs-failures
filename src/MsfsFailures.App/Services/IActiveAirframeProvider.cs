namespace MsfsFailures.App.Services;

/// <summary>
/// Resolves the currently active airframe based on available data sources.
/// Fires an event when the active airframe changes.
/// </summary>
public interface IActiveAirframeProvider
{
    /// <summary>Resolves the currently active airframe ID, or null if none yet.</summary>
    Task<Guid?> GetActiveAirframeIdAsync(CancellationToken ct = default);

    /// <summary>Fires when the active airframe changes.</summary>
    event EventHandler<Guid?>? ActiveAirframeChanged;
}

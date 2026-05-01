using MsfsFailures.Data.Entities;

namespace MsfsFailures.Data.Repositories;

/// <summary>
/// Minimum repository surface needed for v1 fleet operations.
/// </summary>
public interface IFleetRepository
{
    Task<IReadOnlyList<Airframe>> GetAllAirframesAsync(CancellationToken ct = default);
    Task<Airframe?> GetAirframeAsync(Guid id, CancellationToken ct = default);
    Task AddSessionAsync(Session session, CancellationToken ct = default);
    Task<IReadOnlyList<Squawk>> GetOpenSquawksAsync(Guid airframeId, CancellationToken ct = default);

    // ── v1 tick-loop extensions ──────────────────────────────────────────────

    Task<Airframe?> GetAirframeByTailAsync(string tail, CancellationToken ct = default);
    Task<IReadOnlyList<Component>> GetComponentsForAirframeAsync(Guid airframeId, CancellationToken ct = default);
    Task<IReadOnlyList<ComponentTemplate>> GetTemplatesForModelAsync(Guid modelRefId, CancellationToken ct = default);
    Task<IReadOnlyList<FailureMode>> GetFailureModesForTemplatesAsync(IReadOnlyList<Guid> templateIds, CancellationToken ct = default);
    Task<IReadOnlyList<Accelerator>> GetAcceleratorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Consumable>> GetConsumablesForAirframeAsync(Guid airframeId, CancellationToken ct = default);

    /// <summary>
    /// Applies accumulated tick deltas to the airframe and its components/consumables.
    /// Increments cumulative hobbs/cycles counters; clamps wear and consumable level to [0..1].
    /// A single SaveChangesAsync is issued per call.
    /// </summary>
    Task ApplyTickResultAsync(
        Guid airframeId,
        double hobbsHoursDelta,
        int cyclesDelta,
        IReadOnlyDictionary<Guid, double> componentWear,
        IReadOnlyDictionary<Guid, double> consumableLevels,
        CancellationToken ct = default);

    /// <summary>Opens a new flight session and persists it immediately.</summary>
    Task<Session> StartSessionAsync(Guid airframeId, double hobbsAtStart, CancellationToken ct = default);

    /// <summary>Closes an existing session with final hobbs, max-G, and hard-landing count.</summary>
    Task EndSessionAsync(Guid sessionId, double hobbsAtEnd, double maxG, int hardLandings, CancellationToken ct = default);
}

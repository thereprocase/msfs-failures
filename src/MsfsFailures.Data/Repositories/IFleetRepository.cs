using MsfsFailures.Core;
using Entities = MsfsFailures.Data.Entities;
using CoreSquawkStatus = MsfsFailures.Core.SquawkStatus;

namespace MsfsFailures.Data.Repositories;

/// <summary>
/// Minimum repository surface needed for v1 fleet operations.
/// </summary>
public interface IFleetRepository
{
    Task<IReadOnlyList<Entities.Airframe>> GetAllAirframesAsync(CancellationToken ct = default);
    Task<Entities.Airframe?> GetAirframeAsync(Guid id, CancellationToken ct = default);
    Task AddSessionAsync(Entities.Session session, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Squawk>> GetOpenSquawksAsync(Guid airframeId, CancellationToken ct = default);

    // ── v1 tick-loop extensions ──────────────────────────────────────────────

    Task<Entities.Airframe?> GetAirframeByTailAsync(string tail, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Component>> GetComponentsForAirframeAsync(Guid airframeId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.ComponentTemplate>> GetTemplatesForModelAsync(Guid modelRefId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.FailureMode>> GetFailureModesForTemplatesAsync(IReadOnlyList<Guid> templateIds, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Accelerator>> GetAcceleratorsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Entities.Consumable>> GetConsumablesForAirframeAsync(Guid airframeId, CancellationToken ct = default);

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
    Task<Entities.Session> StartSessionAsync(Guid airframeId, double hobbsAtStart, CancellationToken ct = default);

    /// <summary>Closes an existing session with final hobbs, max-G, and hard-landing count.</summary>
    Task EndSessionAsync(Guid sessionId, double hobbsAtEnd, double maxG, int hardLandings, CancellationToken ct = default);

    /// <summary>
    /// Gets all maintenance actions for an airframe, ordered by PerformedAt descending.
    /// </summary>
    Task<IReadOnlyList<Entities.MaintenanceAction>> GetMaintenanceActionsForAirframeAsync(Guid airframeId, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent session for an airframe (by EndedAt or StartedAt, latest first).
    /// Returns null if no sessions exist.
    /// </summary>
    Task<Entities.Session?> GetMostRecentSessionAsync(Guid airframeId, CancellationToken ct = default);

    /// <summary>
    /// Counts squawks for an airframe with a specific status.
    /// </summary>
    Task<int> CountSquawksAsync(Guid airframeId, CoreSquawkStatus status, CancellationToken ct = default);

    /// <summary>
    /// Gets all squawks for an airframe (regardless of status).
    /// </summary>
    Task<IReadOnlyList<Entities.Squawk>> GetSquawksForAirframeAsync(Guid airframeId, CancellationToken ct = default);

    /// <summary>
    /// Gets the airframe ID with the currently open (in-flight) session.
    /// Sessions with EndedAt == null are considered active. Returns the most recent by StartedAt.
    /// </summary>
    Task<Guid?> GetAirframeIdWithOpenSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all ModelRefs from the database.
    /// </summary>
    Task<IReadOnlyList<Entities.ModelRef>> GetAllModelRefsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an airframe by ModelRefId, preferring the one with the most hours (most recently flown).
    /// Returns null if no airframe is found for the given ModelRefId.
    /// </summary>
    Task<Entities.Airframe?> GetAirframeByModelRefAsync(Guid modelRefId, CancellationToken ct = default);

    // ── Squawk write operations ──────────────────────────────────────────────

    /// <summary>Opens a new squawk against an airframe and returns its new Guid.</summary>
    Task<Guid> AddSquawkAsync(
        Guid airframeId,
        string component,
        string summary,
        int severity,
        bool melDeferrable,
        string notes,
        double hoursAtOpen,
        CancellationToken ct = default);

    /// <summary>
    /// Partially updates a squawk. Only non-null parameters are applied.
    /// </summary>
    Task UpdateSquawkAsync(
        Guid squawkId,
        string? component,
        string? summary,
        string? notes,
        int? status,
        DateTimeOffset? deferredUntil,
        CancellationToken ct = default);

    /// <summary>Hard-deletes a squawk record.</summary>
    Task DeleteSquawkAsync(Guid squawkId, CancellationToken ct = default);

    /// <summary>Gets all squawks across all airframes (including their Airframe nav property).</summary>
    Task<IReadOnlyList<Entities.Squawk>> GetAllSquawksAsync(CancellationToken ct = default);
}

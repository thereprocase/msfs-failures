namespace MsfsFailures.Core.Wear;

/// <summary>
/// The output of a single WearEngine tick: deltas to apply to persistent state.
/// All values are additive increments — callers accumulate them into totals.
/// </summary>
public sealed record WearTickResult(
    /// <summary>
    /// Hobbs hours increment for this tick.
    /// Positive whenever the engine is considered running per <see cref="WearEngine.HobbsRpmThreshold"/>.
    /// </summary>
    double HobbsHoursDelta,

    /// <summary>
    /// Landing cycle increment. Typically 0 or 1 per tick; 1 on touchdown event ticks.
    /// </summary>
    int CyclesDelta,

    /// <summary>
    /// Per-component wear increments, keyed by <see cref="Component.Id"/>.
    /// Values are additive to <see cref="Component.Wear"/> [0..1].
    /// Components with no change are omitted.
    /// </summary>
    IReadOnlyDictionary<Guid, double> ComponentWearDeltas,

    /// <summary>
    /// Per-consumable level decrements (negative = consumed), keyed by <see cref="Consumable.Id"/>.
    /// Values are fractions of capacity (e.g., -0.001 = consumed 0.1% of capacity).
    /// Consumables with no change are omitted.
    /// </summary>
    IReadOnlyDictionary<Guid, double> ConsumableLevelDeltas,

    /// <summary>
    /// Informational notes generated during the tick (e.g., "hard landing -620fpm").
    /// Intended for FailureEngine to act on or for logging.
    /// </summary>
    IReadOnlyList<string> Notes);

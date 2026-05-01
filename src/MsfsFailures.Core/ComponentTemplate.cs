namespace MsfsFailures.Core;

/// <summary>
/// A reusable component definition shared by all airframes of the same ModelRef.
/// Defines wear curves, MTBF, replacement intervals, and consumable associations.
/// </summary>
public record ComponentTemplate
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to ModelRef; all components of a type share one set of templates.</summary>
    public required Guid ModelRefId { get; init; }

    /// <summary>Physical category (Engine, HotSection, OilSystem, etc.).</summary>
    public required ComponentCategory Category { get; init; }

    /// <summary>Human-readable name (e.g., "Engine 1", "Main Landing Gear", "Oil System").</summary>
    public required string Name { get; init; }

    /// <summary>Mean time between failures in hours (base, before accelerator factors).</summary>
    public required double MtbfHours { get; init; }

    /// <summary>
    /// Raw JSON string encoding wear curve shape.
    /// Structure: { "type": "exponential|linear|custom", "parameters": {...} }
    /// WearEngine interprets to map hours/cycles/stress → wear level [0..1].
    /// Core stores as opaque JSON.
    /// </summary>
    public required string WearCurve { get; init; }

    /// <summary>If this component is consumable, which kind (Oil, Tire, BrakePad, etc.). None if not consumable.</summary>
    public required ConsumableKind ConsumableKind { get; init; }

    /// <summary>Recommended replacement interval in hours (0 if N/A).</summary>
    public required double ReplaceIntervalHours { get; init; }

    /// <summary>Recommended replacement interval in cycles (0 if N/A).</summary>
    public required int ReplaceIntervalCycles { get; init; }
}

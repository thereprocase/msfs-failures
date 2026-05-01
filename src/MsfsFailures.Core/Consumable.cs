namespace MsfsFailures.Core;

/// <summary>
/// A consumable resource (oil, hydraulic fluid, etc.) tracked per airframe.
/// Level is maintained as a normalized ratio [0..1] relative to capacity.
/// </summary>
public record Consumable
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to Airframe this consumable is tracked for.</summary>
    public required Guid AirframeId { get; init; }

    /// <summary>Type of consumable (Oil, HydraulicFluid, BrakePad, Tire, BatterySoh).</summary>
    public required ConsumableKind Kind { get; init; }

    /// <summary>Current level as a fraction [0..1] of capacity. 0 = empty, 1 = full.</summary>
    public required double Level { get; init; }

    /// <summary>Maximum capacity in units relevant to the kind (gallons, quarts, amps-hours, etc.).</summary>
    public required double Capacity { get; init; }

    /// <summary>When this consumable was last topped up or refilled.</summary>
    public required DateTimeOffset LastTopUpAt { get; init; }
}

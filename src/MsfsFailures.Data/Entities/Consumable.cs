namespace MsfsFailures.Data.Entities;

public class Consumable
{
    public Guid Id { get; set; }
    public Guid AirframeId { get; set; }

    /// <summary>Maps to Core.ConsumableKind enum (stored as int).</summary>
    public int Kind { get; set; }

    public double Level { get; set; }
    public double Capacity { get; set; }
    public DateTimeOffset LastTopUpAt { get; set; }

    // Navigation
    public Airframe Airframe { get; set; } = null!;
}

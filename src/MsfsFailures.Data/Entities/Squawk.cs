namespace MsfsFailures.Data.Entities;

public class Squawk
{
    public Guid Id { get; set; }
    public Guid AirframeId { get; set; }
    public Guid? FailureModeId { get; set; }
    public DateTimeOffset Opened { get; set; }
    public DateTimeOffset? DeferredUntil { get; set; }

    /// <summary>Maps to Core.SquawkStatus enum (stored as int).</summary>
    public int Status { get; set; }

    public string Notes { get; set; } = string.Empty;
    public double HoursAtOpen { get; set; }

    // Navigation
    public Airframe Airframe { get; set; } = null!;
    public FailureMode? FailureMode { get; set; }
}

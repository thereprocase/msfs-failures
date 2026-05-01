namespace MsfsFailures.Data.Entities;

public class MaintenanceAction
{
    public Guid Id { get; set; }
    public Guid AirframeId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ComponentsTouchedJson { get; set; } = string.Empty;
    public double HoursAtAction { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Navigation
    public Airframe Airframe { get; set; } = null!;
}

namespace MsfsFailures.Core;

/// <summary>
/// A maintenance action performed on an airframe (inspection, repair, replacement, etc.).
/// Tracks what was done, which components were touched, and when.
/// </summary>
public record MaintenanceAction
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to Airframe this maintenance was performed on.</summary>
    public required Guid AirframeId { get; init; }

    /// <summary>Description of the action (e.g., "100-hour inspection", "Engine overhaul", "Oil change").</summary>
    public required string Action { get; init; }

    /// <summary>
    /// Raw JSON string listing component IDs that were touched/serviced.
    /// Structure: ["{guid1}", "{guid2}", ...]
    /// Opaque to Core; used for maintenance history tracking.
    /// </summary>
    public required string ComponentsTouched { get; init; }

    /// <summary>Airframe Hobbs hours at the time this action was performed.</summary>
    public required double HoursAtAction { get; init; }

    /// <summary>When this maintenance action was recorded.</summary>
    public required DateTimeOffset PerformedAt { get; init; }

    /// <summary>Optional notes about the work (inspector comments, parts replaced, findings, etc.).</summary>
    public required string Notes { get; init; }
}

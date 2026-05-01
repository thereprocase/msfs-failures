namespace MsfsFailures.Core;

/// <summary>
/// A reported failure (squawk) on a specific airframe.
/// Tracks status (Open/Deferred/Closed) and MEL deferral deadline.
/// </summary>
public record Squawk
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to Airframe this squawk is logged against.</summary>
    public required Guid AirframeId { get; init; }

    /// <summary>Foreign key to FailureMode that triggered this squawk. Nullable for manually-logged issues.</summary>
    public required Guid? FailureModeId { get; init; }

    /// <summary>When this squawk was opened (failure detected or manually reported).</summary>
    public required DateTimeOffset Opened { get; init; }

    /// <summary>If deferred under MEL, the deadline after which repair is required. Null if not deferred.</summary>
    public required DateTimeOffset? DeferredUntil { get; init; }

    /// <summary>Current status (Open, Deferred, Closed).</summary>
    public required SquawkStatus Status { get; init; }

    /// <summary>Crew notes about the squawk (observations, workarounds, etc.).</summary>
    public required string Notes { get; init; }

    /// <summary>Airframe Hobbs hours at the time the squawk was opened (for trend tracking).</summary>
    public required double HoursAtOpen { get; init; }
}

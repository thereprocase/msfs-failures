namespace MsfsFailures.Core;

/// <summary>
/// A specific instance of a ComponentTemplate installed on a particular Airframe.
/// Tracks wear level, condition, and service history.
/// </summary>
public record Component
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to Airframe this component is installed on.</summary>
    public required Guid AirframeId { get; init; }

    /// <summary>Foreign key to ComponentTemplate defining this component's properties.</summary>
    public required Guid TemplateId { get; init; }

    /// <summary>Hours accumulated on this component (may differ from airframe total if replaced).</summary>
    public required double Hours { get; init; }

    /// <summary>Cycles accumulated on this component.</summary>
    public required int Cycles { get; init; }

    /// <summary>Wear level normalized to [0..1]. 1.0 indicates failure imminent or occurred.</summary>
    public required double Wear { get; init; }

    /// <summary>Human-readable condition label (e.g., "serviceable", "worn", "overhaul due").</summary>
    public required string Condition { get; init; }

    /// <summary>When this component was last serviced/replaced.</summary>
    public required DateTimeOffset LastServicedAt { get; init; }

    /// <summary>When this component was installed on the airframe.</summary>
    public required DateTimeOffset InstalledAt { get; init; }
}

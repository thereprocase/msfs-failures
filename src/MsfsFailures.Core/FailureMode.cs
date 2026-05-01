namespace MsfsFailures.Core;

/// <summary>
/// A discrete failure mode that can occur on a component.
/// Defines sim binding (how to inject into MSFS), severity, and MEL properties.
/// Follows the C208B pattern: wear → MTBF reduction → Exponential random draw.
/// </summary>
public record FailureMode
{
    /// <summary>Unique identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Foreign key to ComponentTemplate this failure mode can affect.</summary>
    public required Guid TemplateId { get; init; }

    /// <summary>Human-readable name (e.g., "Engine Fire", "Alternator Failure", "Magneto Failure").</summary>
    public required string Name { get; init; }

    /// <summary>How this failure is injected into the sim (SimConnect event, L:Var write, internal, etc.).</summary>
    public required SimBindingKind SimBindingKind { get; init; }

    /// <summary>
    /// The payload for the binding: SimConnect event name, L:Var address + value, K:Event string, etc.
    /// Opaque to Core; consumed by Sim layer.
    /// </summary>
    public required string SimBindingPayload { get; init; }

    /// <summary>Severity from crew perspective (Annunciator, Warning, Caution, Grounding).</summary>
    public required FailureSeverity Severity { get; init; }

    /// <summary>Estimated repair/replacement time in hours (used for scheduling and deferred maintenance).</summary>
    public required double RepairHours { get; init; }

    /// <summary>Whether this failure can be deferred under MEL (Minimum Equipment List).</summary>
    public required bool MelDeferrable { get; init; }
}

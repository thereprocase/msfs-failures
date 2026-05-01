namespace MsfsFailures.Data.Entities;

public class FailureMode
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Maps to Core.SimBindingKind enum (stored as int).</summary>
    public int SimBindingKind { get; set; }

    public string SimBindingPayload { get; set; } = string.Empty;

    /// <summary>Maps to Core.FailureSeverity enum (stored as int).</summary>
    public int Severity { get; set; }

    public double RepairHours { get; set; }
    public bool MelDeferrable { get; set; }

    // Navigation
    public ComponentTemplate Template { get; set; } = null!;
    public List<Squawk> Squawks { get; set; } = [];
}

namespace MsfsFailures.Data.Entities;

public class ComponentTemplate
{
    public Guid Id { get; set; }
    public Guid ModelRefId { get; set; }

    /// <summary>Maps to Core.ComponentCategory enum (stored as int).</summary>
    public int Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public double MtbfHours { get; set; }
    public string WearCurveJson { get; set; } = string.Empty;

    /// <summary>Maps to Core.ConsumableKind enum (stored as int).</summary>
    public int ConsumableKind { get; set; }

    public double? ReplaceIntervalHours { get; set; }
    public int? ReplaceIntervalCycles { get; set; }

    // Navigation
    public ModelRef ModelRef { get; set; } = null!;
    public List<Component> Components { get; set; } = [];
    public List<FailureMode> FailureModes { get; set; } = [];
}

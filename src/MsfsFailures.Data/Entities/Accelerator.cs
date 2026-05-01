namespace MsfsFailures.Data.Entities;

public class Accelerator
{
    public Guid Id { get; set; }

    /// <summary>Maps to Core.ComponentCategory enum (stored as int).</summary>
    public int Category { get; set; }

    public string Variable { get; set; } = string.Empty;
    public string FormulaJson { get; set; } = string.Empty;
}

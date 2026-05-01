namespace MsfsFailures.Data.Entities;

public class VarBinding
{
    public Guid Id { get; set; }
    public Guid ModelRefId { get; set; }
    public string LogicalName { get; set; } = string.Empty;

    /// <summary>Maps to Core.VarSource enum (stored as int). A:, L:, K:, SimConnect, etc.</summary>
    public int Source { get; set; }

    public string Expression { get; set; } = string.Empty;

    // Navigation
    public ModelRef ModelRef { get; set; } = null!;
}

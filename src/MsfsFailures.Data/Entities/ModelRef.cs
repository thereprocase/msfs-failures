namespace MsfsFailures.Data.Entities;

public class ModelRef
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string SimMatchRulesJson { get; set; } = string.Empty;

    // Navigation
    public List<ComponentTemplate> ComponentTemplates { get; set; } = [];
    public List<VarBinding> VarBindings { get; set; } = [];
}

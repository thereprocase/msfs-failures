namespace MsfsFailures.Data.Entities;

public class Component
{
    public Guid Id { get; set; }
    public Guid AirframeId { get; set; }
    public Guid TemplateId { get; set; }
    public double Hours { get; set; }
    public int Cycles { get; set; }
    public double Wear { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTimeOffset LastServicedAt { get; set; }
    public DateTimeOffset InstalledAt { get; set; }

    // Navigation
    public Airframe Airframe { get; set; } = null!;
    public ComponentTemplate Template { get; set; } = null!;
}

namespace MsfsFailures.Data.Entities;

public class Airframe
{
    public Guid Id { get; set; }
    public string Tail { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid ModelRefId { get; set; }
    public double TotalHobbsHours { get; set; }
    public int TotalCycles { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public ModelRef ModelRef { get; set; } = null!;
    public List<Component> Components { get; set; } = [];
    public List<Consumable> Consumables { get; set; } = [];
    public List<Squawk> Squawks { get; set; } = [];
    public List<MaintenanceAction> MaintenanceActions { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];
}

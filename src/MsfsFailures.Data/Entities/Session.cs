namespace MsfsFailures.Data.Entities;

public class Session
{
    public Guid Id { get; set; }
    public Guid AirframeId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public double HobbsStart { get; set; }
    public double? HobbsEnd { get; set; }
    public double MaxG { get; set; }
    public int HardLandings { get; set; }
    public string OvertempEventsJson { get; set; } = string.Empty;

    // Navigation
    public Airframe Airframe { get; set; } = null!;
}

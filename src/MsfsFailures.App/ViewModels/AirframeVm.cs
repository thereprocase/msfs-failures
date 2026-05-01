namespace MsfsFailures.App.ViewModels;

public sealed record LastFlightVm(
    string Date,
    string Duration,
    double MaxG,
    int HardLandings,
    int Overtemps);

public sealed record ConsumablesVm(
    double Oil,
    double Tires,
    double Brakes,
    double Battery,
    double Hydraulic);

public sealed class AirframeVm
{
    public required string Id { get; init; }
    public required string Tail { get; init; }
    public required string Type { get; init; }
    public required string Model { get; init; }
    public required string Nickname { get; init; }
    public required AirframeStatus Status { get; init; }
    public required double Hours { get; init; }
    public required int Cycles { get; init; }
    public required double HobbsSinceMx { get; init; }
    public required double NextInspectionHrs { get; init; }
    public required int OpenSquawks { get; init; }
    public required int Deferred { get; init; }
    public required ConsumablesVm Consumables { get; init; }
    public required LastFlightVm LastFlight { get; init; }
    public bool Live { get; init; }
    public LiveStateVm? LiveState { get; init; }

    // Display helpers
    public string StatusLabel => Status.ToLabel();
    public string HoursStr    => Hours.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    public string CyclesStr   => Cycles.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    public string HobbsSinceMxStr     => HobbsSinceMx.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    public string NextInspectionStr   => NextInspectionHrs.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "h";
    public bool   InspectionAlert     => NextInspectionHrs < 5;
    public string SquawksDisplay
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(OpenSquawks > 0 ? $"!{OpenSquawks}" : "—");
            if (Deferred > 0) sb.Append(" /").Append(Deferred).Append('d');
            return sb.ToString();
        }
    }
    public string LastFlightDisplay => $"{LastFlight.Date} · {LastFlight.Duration}";

    public IEnumerable<KeyValuePair<string,double>> ConsumablePairs =>
    [
        new("OIL",  Consumables.Oil),
        new("TIRE", Consumables.Tires),
        new("BRK",  Consumables.Brakes),
        new("BAT",  Consumables.Battery),
        new("HYD",  Consumables.Hydraulic),
    ];
}

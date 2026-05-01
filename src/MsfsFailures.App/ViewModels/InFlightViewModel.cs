using CommunityToolkit.Mvvm.ComponentModel;

namespace MsfsFailures.App.ViewModels;

public sealed record HealthIndex(string Label, string Sub, double Value);

public sealed record RouteWaypoint(string Id, string Time, double Position);

public sealed record FlightEvent(string Time, string Kind, string Message);

public sealed record HabitItem(string Name, string ValueDisplay, string Unit, string Note, string Tone);

public sealed record DriftItem(string Name, string Now, string Base, string Unit, string Delta, string Tone);

public sealed record MxDebtItem(string Name, string Due, string Note, string Severity);

public sealed record PrecursorItem(string Name, double Value, string Scale, string Current);

public sealed record ProjectionItem(string Name, string Delta, string Detail, string Tone);

public sealed class InFlightViewModel : ObservableObject
{
    // Aircraft constants
    public string Tail        { get; } = "N208RC";
    public string Type        { get; } = "C208B";
    public string Model       { get; } = "Cessna 208B Grand Caravan EX";
    public string Binding     { get; } = "C208B.default v1.2";
    public string Engine      { get; } = "PT6A-140";
    public double HobbsStart  { get; } = 1842.6;
    public double HobbsNow    { get; } = 1844.1;
    public int    Cycles      { get; } = 2106;
    public double HobbsDelta  => HobbsNow - HobbsStart;

    // Live state
    public string Phase    { get; } = "CRUISE";
    public int Altitude    { get; } = 9500;
    public int Vsi         { get; } = 0;
    public int Ias         { get; } = 168;
    public int Tas         { get; } = 188;
    public int Gs          { get; } = 174;
    public int Hdg         { get; } = 281;
    public int Oat         { get; } = -4;
    public int FuelLb      { get; } = 1842;
    public int FuelCapLb   { get; } = 2240;
    public int FuelFlow    { get; } = 412;
    public double Ng       { get; } = 91.4;
    public int Np          { get; } = 1900;
    public int Itt         { get; } = 685;
    public int IttLimit    { get; } = 825;
    public int IttCaution  { get; } = 765;
    public int Torque      { get; } = 1410;
    public int TorqueLimit { get; } = 1970;
    public int TorqueCaution { get; } = 1865;
    public int OilTemp     { get; } = 79;
    public int OilPress    { get; } = 87;
    public double OilQt    { get; } = 11.4;

    public string AltDisplay   => Altitude.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " ft";
    public string IasDisplay   => Ias + " kt";
    public string TasDisplay   => Tas + " kt";
    public string GsDisplay    => Gs + " kt";
    public string HdgDisplay   => Hdg.ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + "°";
    public string OatDisplay   => Oat + "°C";
    public string IttDisplay   => Itt + "°C";
    public string TqDisplay    => Torque + " ft·lb";
    public string FfDisplay    => FuelFlow + " pph";
    public string FuelLbDisplay => FuelLb.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " lb";
    public string EnduranceHours => (FuelLb / (double)FuelFlow).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
    public string ReserveAtArrival => "+" + (((double)FuelLb / FuelFlow) - 0.86 - 0.32).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + "h";

    // Health indices
    public IReadOnlyList<HealthIndex> Health { get; } =
    [
        new("ENGINE",        "condition index",                          0.74),
        new("HOT SECTION",   "1382 h to HSI",                            0.82),
        new("COMPRESSOR",    "wash due",                                 0.61),
        new("OIL SYSTEM",    "11.4 / 14 qt",                             0.88),
        new("FUEL",          "clean · last sample 4.2 h ago",            0.94),
        new("PROPELLER",     "erosion + governor drift",                 0.79),
        new("GEAR · BRAKES", "brakes 55% · tires 62%",                   0.66),
        new("ELECTRICAL",    "battery SOH 91%",                          0.91),
    ];

    public double EngineHealth        => Health[0].Value;
    public string EngineHealthPercent => ((int)Math.Round(Health[0].Value * 100)).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Thermal
    public double ThermalBudget { get; } = 0.72;
    public string ThermalBudgetPct => ((int)Math.Round(ThermalBudget * 100)).ToString() + "%";
    public string ThermalBelowLimit => (IttLimit - Itt).ToString() + "°C BELOW LIMIT";

    // Route
    public IReadOnlyList<RouteWaypoint> Route { get; } =
    [
        new("KAPA", "13:54Z", 0.00),
        new("DBL",  "14:14Z", 0.25),
        new("JNC",  "14:42Z", 0.50),
        new("LKV",  "15:18Z", 0.75),
        new("KSEZ", "15:46Z", 1.00),
    ];
    public double RouteProgress { get; } = 0.42;

    // Events
    public int EventsCount => Events.Count;

    public IReadOnlyList<FlightEvent> Events { get; } =
    [
        new("14:08:11", "phase", "LIFTOFF · gear/strut wear sample taken (vs 412 fpm, weight 8624 lb)"),
        new("14:08:42", "wear",  "Cycle counter → 2106 · landing-gear cycle logged"),
        new("14:14:02", "warn",  "ITT peaked 712°C in climb · added 0.4 °C·s to overtemp debt"),
        new("14:18:30", "phase", "TOC reached · cruise wear profile engaged"),
        new("14:19:04", "note",  "Compressor efficiency held at 87.3% (was 92.0% new)"),
        new("14:31:18", "wear",  "Tick · oil consumed 0.18 qt this leg · within trend band"),
        new("15:02:55", "note",  "Same-power climb cost +18 fpm fuel for −12 fpm climb vs new-engine baseline"),
    ];

    // Trends
    public IReadOnlyList<double> IttTrend    { get; } = [612,634,656,672,684,698,712,706,694,688,684,682,684,685,686,684,683,685,686,685];
    public IReadOnlyList<double> TorqueTrend { get; } = [880,1100,1320,1540,1700,1820,1865,1820,1690,1560,1480,1420,1410,1408,1410,1412,1410,1411,1410,1410];
    public IReadOnlyList<double> OilTrend    { get; } = [13.2,13.0,12.8,12.6,12.5,12.3,12.1,12.0,11.9,11.8,11.7,11.6,11.5,11.5,11.4];

    // Pilot habits
    public IReadOnlyList<HabitItem> Habits { get; } =
    [
        new("Hot starts",          "0",    "last 30 d",       "streak: 47 starts clean",                "good"),
        new("Torque exceedances",  "2",    "events",          "last: 14:14 · +95 ft·lb · 4.1 s",        "warn"),
        new("ITT overtemp debt",   "0.4",  "°C·s",            "this leg only · cleared at TOC",         "good"),
        new("Mean climb torque",   "1750", "ft·lb",           "+8% above book climb · burn cost +1.4%","warn"),
        new("Hard landings",       "0",    ">500 fpm vs",     "softest 30 d: −198 fpm",                 "good"),
        new("Beta on ground only", "100",  "% compliance",    "no in-air reverse detected",             "good"),
        new("Compressor wash",     "47",   "h since wash",    "service threshold · efficiency −4.7%",   "warn"),
        new("Fuel sample interval","4.2",  "h since",         "auto-prompt at 10 h",                    "good"),
    ];

    // Performance drift
    public IReadOnlyList<DriftItem> Drift { get; } =
    [
        new("Climb rate · same TQ",   "712",  "850",  "fpm", "−16%",  "amber"),
        new("Cruise TAS · same TQ",   "188",  "195",  "kt",  "−3.6%", "good"),
        new("Fuel burn · same power", "412",  "388",  "pph", "+6.2%", "amber"),
        new("Compressor efficiency",  "87.3", "92.0", "%",   "−4.7%", "amber"),
    ];

    // Latent precursors
    public IReadOnlyList<PrecursorItem> Precursors { get; } =
    [
        new("Compressor fouling",  0.39, "NEW → FOULED",  "EFFICIENCY 87.3%"),
        new("Fuel nozzle coking",  0.22, "CLEAN → COKED", "spray pattern within trend"),
        new("Bearing condition",   0.18, "NEW → WEAR",    "oil debris monitor 0.3 ppm"),
        new("Prop blade erosion",  0.31, "NEW → ERODED",  "rain·dust flights: 18"),
        new("Brake thermal load",  0.10, "COLD → FADE",   "cooled · idle in cruise"),
        new("Strut packing wear",  0.28, "NEW → WEEPING", "last hard ldg 30 d ago"),
    ];

    // Maintenance debt
    public IReadOnlyList<MxDebtItem> MxDebt { get; } =
    [
        new("Compressor wash",    "OVERDUE", "efficiency loss accelerates", "amber"),
        new("Fuel sump · L tank", "12 h",    "next preflight prompt",       "good"),
        new("100-hr inspection",  "85.8 h",  "phase items unbundled",       "good"),
        new("HSI · hot section",  "1382 h",  "~1800 h interval reached",    "good"),
        new("Oil change (50 h)",  "14 h",    "on schedule",                 "good"),
        new("Vacuum filter",      "DEFERRED","MEL · until 2026-05-10",      "dim"),
    ];

    // Post-flight projection
    public IReadOnlyList<ProjectionItem> Projection { get; } =
    [
        new("Hot section life", "−1.4 h",  "incl. climb ITT 712°C peak",     "good"),
        new("Compressor eff.",  "−0.04%",  "dust ingestion · cruise alt",    "amber"),
        new("Oil consumed",     "0.31 qt", "@ 0.12 qt/h · 2.6 h leg",        "good"),
        new("Tires",            "−0.4%",   "proj. 142 kt at touchdown",      "good"),
        new("Brakes",           "−0.6%",   "9100 ft rwy · light braking",    "good"),
        new("Cycles",           "+1",      "gear · pressurization n/a",      "good"),
    ];

    // Live strip pairs
    public IEnumerable<KeyValuePair<string,string>> LiveStripPairs
    {
        get
        {
            yield return new("ALT", AltDisplay);
            yield return new("IAS", IasDisplay);
            yield return new("TAS", TasDisplay);
            yield return new("GS",  GsDisplay);
            yield return new("HDG", HdgDisplay);
            yield return new("OAT", OatDisplay);
            yield return new("ITT", IttDisplay);
            yield return new("TQ",  TqDisplay);
            yield return new("FF",  FfDisplay);
        }
    }
}

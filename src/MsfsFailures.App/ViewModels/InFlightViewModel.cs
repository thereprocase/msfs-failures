using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsfsFailures.App.Services;
using MsfsFailures.Core;
using MsfsFailures.Core.Wear;
using MsfsFailures.Data.Repositories;
using MsfsFailures.Sim;
using DataEntities = MsfsFailures.Data.Entities;

namespace MsfsFailures.App.ViewModels;

public sealed record HealthIndex(string Label, string Sub, double Value);

public sealed record RouteWaypoint(string Id, string Time, double Position);

public sealed record FlightEvent(string Time, string Kind, string Message);

public sealed record HabitItem(string Name, string ValueDisplay, string Unit, string Note, string Tone);

public sealed record DriftItem(string Name, string Now, string Base, string Unit, string Delta, string Tone);

public sealed record MxDebtItem(string Name, string Due, string Note, string Severity);

public sealed record PrecursorItem(string Name, double Value, string Scale, string Current);

public sealed record ProjectionItem(string Name, string Delta, string Detail, string Tone);

/// <summary>
/// Live IN FLIGHT view model. Subscribes to <see cref="ISimBus.SampleStream"/> and
/// <see cref="ISimBus.StatusStream"/>; updates observable properties on each 4 Hz tick.
/// Static analysis surfaces (health indices, habits, drift, precursors, debt, projection,
/// route, events) remain hardcoded in v1 — those require more plumbing beyond sample data.
/// </summary>
public sealed partial class InFlightViewModel : ObservableObject, IDisposable
{
    private const int TrendBufferSize = 60;
    private const int HealthRefreshIntervalSeconds = 30;

    private readonly ILogger<InFlightViewModel> _log;
    private readonly IDisposable _sampleSub;
    private readonly IDisposable _statusSub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IActiveAirframeProvider _airframeProvider;
    private readonly System.Timers.Timer _healthRefreshTimer;
    private CancellationTokenSource? _healthCts;

    // Rolling trend queues — updated on sample thread, read by UI thread via ObservableCollection.
    private readonly Queue<double> _ittTrendQueue   = new(TrendBufferSize + 1);
    private readonly Queue<double> _torqueTrendQueue = new(TrendBufferSize + 1);

    // Default hardcoded health (fallback when no DB data).
    private static readonly IReadOnlyList<HealthIndex> DefaultHealth =
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

    public InFlightViewModel(
        ISimBus bus,
        ILogger<InFlightViewModel> log,
        IServiceScopeFactory scopeFactory,
        IActiveAirframeProvider airframeProvider)
    {
        _log = log;
        _scopeFactory = scopeFactory;
        _airframeProvider = airframeProvider;

        // Seed trend buffers with initial static values (replaced by live data on first tick).
        foreach (var v in new double[] { 612,634,656,672,684,698,712,706,694,688,684,682,684,685,686,684,683,685,686,685 })
            _ittTrendQueue.Enqueue(v);
        foreach (var v in new double[] { 880,1100,1320,1540,1700,1820,1865,1820,1690,1560,1480,1420,1410,1408,1410,1412,1410,1411,1410,1410 })
            _torqueTrendQueue.Enqueue(v);

        IttTrend    = new ObservableCollection<double>(_ittTrendQueue);
        TorqueTrend = new ObservableCollection<double>(_torqueTrendQueue);
        Health      = new ObservableCollection<HealthIndex>(DefaultHealth);

        _sampleSub = bus.SampleStream.Subscribe(OnSample, OnSampleError);
        _statusSub = bus.StatusStream.Subscribe(OnStatus, OnStatusError);

        // Subscribe to airframe changes to recompute health.
        _airframeProvider.ActiveAirframeChanged += OnAirframeChanged;

        // Set up periodic health refresh timer.
        _healthRefreshTimer = new System.Timers.Timer(HealthRefreshIntervalSeconds * 1000)
        {
            AutoReset = true
        };
        _healthRefreshTimer.Elapsed += async (s, e) => await RefreshHealthAsync();

        _healthCts = new CancellationTokenSource();

        // Compute health once at startup (after all wiring) and start the timer.
        _ = RefreshHealthAsync();
        _healthRefreshTimer.Start();
    }

    // ── Aircraft constants (fixed for seeded demo aircraft) ──────────────────

    public string Tail       { get; } = "N208RC";
    public string Type       { get; } = "C208B";
    public string Model      { get; } = "Cessna 208B Grand Caravan EX";
    public string Binding    { get; } = "C208B.default v1.2";
    public string Engine     { get; } = "PT6A-140";
    public double HobbsStart { get; } = 1842.6;
    public double HobbsNow   { get; } = 1844.1;
    public int    Cycles     { get; } = 2106;
    public double HobbsDelta => HobbsNow - HobbsStart;

    // ── Live state — observable properties updated from SampleStream ─────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IttDisplay))]
    [NotifyPropertyChangedFor(nameof(ThermalBelowLimit))]
    private int _itt = 685;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TqDisplay))]
    private int _torque = 1410;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IasDisplay))]
    private int _ias = 168;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TasDisplay))]
    private int _tas = 188;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GsDisplay))]
    private int _gs = 174;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HdgDisplay))]
    private int _hdg = 281;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OatDisplay))]
    private int _oat = -4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FfDisplay))]
    private int _fuelFlow = 412;

    [ObservableProperty]
    private int _oilTemp = 79;

    [ObservableProperty]
    private int _oilPress = 87;

    [ObservableProperty]
    private double _oilQt = 11.4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AltDisplay))]
    private int _altitude = 9500;

    [ObservableProperty]
    private int _vsi = 0;

    [ObservableProperty]
    private string _phase = "CRUISE";

    [ObservableProperty]
    private int _fuelLb = 1842;

    // ── Fixed engine limits (not observable — constant for this airframe) ────

    public int IttLimit       { get; } = 825;
    public int IttCaution     { get; } = 765;
    public int TorqueLimit    { get; } = 1970;
    public int TorqueCaution  { get; } = 1865;
    public int FuelCapLb      { get; } = 2240;
    public double Ng          { get; } = 91.4;
    public int Np             { get; } = 1900;

    // ── Derived display strings ──────────────────────────────────────────────

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

    // ── Health indices (observable, computed from active airframe data) ──────

    [ObservableProperty]
    private ObservableCollection<HealthIndex> _health = new(DefaultHealth);

    public double EngineHealth        => Health.Count > 0 ? Health[0].Value : 0.74;
    public string EngineHealthPercent => ((int)Math.Round(EngineHealth * 100)).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // ── Thermal ──────────────────────────────────────────────────────────────

    public double ThermalBudget { get; } = 0.72;
    public string ThermalBudgetPct => ((int)Math.Round(ThermalBudget * 100)).ToString() + "%";
    public string ThermalBelowLimit => (IttLimit - Itt).ToString() + "°C BELOW LIMIT";

    // ── Route (hardcoded v1) ─────────────────────────────────────────────────

    public IReadOnlyList<RouteWaypoint> Route { get; } =
    [
        new("KAPA", "13:54Z", 0.00),
        new("DBL",  "14:14Z", 0.25),
        new("JNC",  "14:42Z", 0.50),
        new("LKV",  "15:18Z", 0.75),
        new("KSEZ", "15:46Z", 1.00),
    ];
    public double RouteProgress { get; } = 0.42;

    // ── Events (hardcoded v1) ────────────────────────────────────────────────

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

    // ── Trends — rolling buffers (live ITT + Torque; OilTrend stubbed TODO) ──

    public ObservableCollection<double> IttTrend    { get; }
    public ObservableCollection<double> TorqueTrend { get; }

    // TODO: aggregate to "qt at preflight per flight" per-session once session tracking lands.
    public IReadOnlyList<double> OilTrend { get; } = [13.2,13.0,12.8,12.6,12.5,12.3,12.1,12.0,11.9,11.8,11.7,11.6,11.5,11.5,11.4];

    // ── Pilot habits (hardcoded v1) ──────────────────────────────────────────

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

    // ── Performance drift (hardcoded v1) ─────────────────────────────────────

    public IReadOnlyList<DriftItem> Drift { get; } =
    [
        new("Climb rate · same TQ",   "712",  "850",  "fpm", "−16%",  "amber"),
        new("Cruise TAS · same TQ",   "188",  "195",  "kt",  "−3.6%", "good"),
        new("Fuel burn · same power", "412",  "388",  "pph", "+6.2%", "amber"),
        new("Compressor efficiency",  "87.3", "92.0", "%",   "−4.7%", "amber"),
    ];

    // ── Latent precursors (hardcoded v1) ─────────────────────────────────────

    public IReadOnlyList<PrecursorItem> Precursors { get; } =
    [
        new("Compressor fouling",  0.39, "NEW → FOULED",  "EFFICIENCY 87.3%"),
        new("Fuel nozzle coking",  0.22, "CLEAN → COKED", "spray pattern within trend"),
        new("Bearing condition",   0.18, "NEW → WEAR",    "oil debris monitor 0.3 ppm"),
        new("Prop blade erosion",  0.31, "NEW → ERODED",  "rain·dust flights: 18"),
        new("Brake thermal load",  0.10, "COLD → FADE",   "cooled · idle in cruise"),
        new("Strut packing wear",  0.28, "NEW → WEEPING", "last hard ldg 30 d ago"),
    ];

    // ── Maintenance debt (hardcoded v1) ──────────────────────────────────────

    public IReadOnlyList<MxDebtItem> MxDebt { get; } =
    [
        new("Compressor wash",    "OVERDUE", "efficiency loss accelerates", "amber"),
        new("Fuel sump · L tank", "12 h",    "next preflight prompt",       "good"),
        new("100-hr inspection",  "85.8 h",  "phase items unbundled",       "good"),
        new("HSI · hot section",  "1382 h",  "~1800 h interval reached",    "good"),
        new("Oil change (50 h)",  "14 h",    "on schedule",                 "good"),
        new("Vacuum filter",      "DEFERRED","MEL · until 2026-05-10",      "dim"),
    ];

    // ── Post-flight projection (hardcoded v1) ────────────────────────────────

    public IReadOnlyList<ProjectionItem> Projection { get; } =
    [
        new("Hot section life", "−1.4 h",  "incl. climb ITT 712°C peak",     "good"),
        new("Compressor eff.",  "−0.04%",  "dust ingestion · cruise alt",    "amber"),
        new("Oil consumed",     "0.31 qt", "@ 0.12 qt/h · 2.6 h leg",        "good"),
        new("Tires",            "−0.4%",   "proj. 142 kt at touchdown",      "good"),
        new("Brakes",           "−0.6%",   "9100 ft rwy · light braking",    "good"),
        new("Cycles",           "+1",      "gear · pressurization n/a",      "good"),
    ];

    // ── Live strip pairs ─────────────────────────────────────────────────────

    public IEnumerable<KeyValuePair<string, string>> LiveStripPairs
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

    // ── Reactive subscription handlers ───────────────────────────────────────

    private void OnSample(FlightTickSample s)
    {
        // Marshal to UI thread via dispatcher if needed.
        // CommunityToolkit ObservableProperty setters are thread-safe for notification,
        // but WPF binding engine expects changes on UI thread for collection updates.
        // Simple properties update fine from any thread in WPF for scalar bindings.

        Ias      = (int)Math.Round(s.IasKt);
        Gs       = (int)Math.Round(s.GsKt);
        Oat      = (int)Math.Round(s.OatC);
        Vsi      = (int)Math.Round(s.VerticalSpeedFpm);
        FuelFlow = (int)Math.Round(s.FuelFlowPph);
        OilTemp  = (int)Math.Round(s.OilTempC);
        OilPress = (int)Math.Round(s.OilPressurePsi);
        Itt      = (int)Math.Round(s.IttC > 0 ? s.IttC : Itt); // keep last if -1
        Torque   = (int)Math.Round(s.TorqueFtLb > 0 ? s.TorqueFtLb : Torque);

        // Derive phase from flight state.
        Phase = DerivePhase(s);

        // Update rolling trend buffers.
        PushTrend(_ittTrendQueue, IttTrend, (double)Itt);
        PushTrend(_torqueTrendQueue, TorqueTrend, (double)Torque);
    }

    private void OnSampleError(Exception ex)
    {
        _log.LogError(ex, "InFlightViewModel: SampleStream error.");
    }

    private void OnStatus(SimStatus status)
    {
        if (status.State == SimConnectionState.Connected)
            _log.LogDebug("InFlightViewModel: sim connected — live data active.");
        else if (status.State == SimConnectionState.Offline)
            _log.LogDebug("InFlightViewModel: sim offline — holding last values.");
    }

    private void OnStatusError(Exception ex)
    {
        _log.LogError(ex, "InFlightViewModel: StatusStream error.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string DerivePhase(FlightTickSample s)
    {
        if (s.OnGround && s.IasKt < 10) return "GROUND";
        if (s.OnGround) return "TAXI";
        if (!s.OnGround && s.VerticalSpeedFpm > 200) return "CLIMB";
        if (!s.OnGround && s.VerticalSpeedFpm < -200) return "DESCENT";
        return "CRUISE";
    }

    private static void PushTrend(Queue<double> queue, ObservableCollection<double> collection, double value)
    {
        queue.Enqueue(value);
        if (queue.Count > TrendBufferSize)
            queue.Dequeue();

        // Sync ObservableCollection to queue contents.
        // For performance, only append and trim rather than rebuilding.
        collection.Add(value);
        while (collection.Count > TrendBufferSize)
            collection.RemoveAt(0);
    }

    // ── Health computation ──────────────────────────────────────────────────

    private void OnAirframeChanged(object? sender, Guid? airframeId)
    {
        _log.LogInformation("InFlightViewModel: active airframe changed to {AirframeId}.", airframeId);
        _ = RefreshHealthAsync();
    }

    private async Task RefreshHealthAsync()
    {
        if (_healthCts?.Token.IsCancellationRequested ?? true)
            return;

        try
        {
            var airframeId = await _airframeProvider.GetActiveAirframeIdAsync(_healthCts.Token);
            if (airframeId is null)
            {
                _log.LogInformation("InFlightViewModel: no active airframe yet.");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
            var components = await repo.GetComponentsForAirframeAsync(airframeId.Value, _healthCts.Token);
            var consumables = await repo.GetConsumablesForAirframeAsync(airframeId.Value, _healthCts.Token);
            var templates = await repo.GetTemplatesForModelAsync(
                (await repo.GetAirframeAsync(airframeId.Value, _healthCts.Token))?.ModelRefId ?? Guid.Empty,
                _healthCts.Token);

            var newHealth = ComputeHealth(components, consumables, templates);

            // Marshal to UI thread.
            if (Application.Current?.Dispatcher is var disp && disp != null)
            {
                disp.Invoke(() =>
                {
                    Health.Clear();
                    foreach (var h in newHealth)
                        Health.Add(h);

                    var healthStr = string.Join(", ", newHealth.Select(h => $"{h.Label}={((int)Math.Round(h.Value * 100))}%"));
                    _log.LogInformation("InFlightViewModel: recomputed health: {Health}.", healthStr);
                });
            }
            else
            {
                // No dispatcher — update synchronously.
                Health.Clear();
                foreach (var h in newHealth)
                    Health.Add(h);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown in progress.
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "InFlightViewModel: failed to recompute health.");
        }
    }

    private IReadOnlyList<HealthIndex> ComputeHealth(
        IReadOnlyList<DataEntities.Component> components,
        IReadOnlyList<DataEntities.Consumable> consumables,
        IReadOnlyList<DataEntities.ComponentTemplate> templates)
    {
        var result = new List<HealthIndex>();

        // Group components by category.
        var componentsByCategory = components
            .GroupBy(c =>
            {
                var template = templates.FirstOrDefault(t => t.Id == c.TemplateId);
                return template != null ? (ComponentCategory)template.Category : ComponentCategory.Other;
            })
            .ToDictionary(g => g.Key, g => g.ToList());

        // Consumable lookup by Kind.
        var consumablesByKind = consumables
            .GroupBy(c => (ConsumableKind)c.Kind)
            .ToDictionary(g => g.Key, g => g.ToList());

        // ENGINE — average (1 - wear) for Engine category components.
        var engineHealth = AverageHealth(componentsByCategory.GetValueOrDefault(ComponentCategory.Engine) ?? new());
        result.Add(new("ENGINE", "condition index", engineHealth));

        // HOT SECTION — average (1 - wear) for HotSection category.
        var hotSectionHealth = AverageHealth(componentsByCategory.GetValueOrDefault(ComponentCategory.HotSection) ?? new());
        var hotsectionSub = hotSectionHealth > 0.5 ? "templates avg" : "HSI service overdue";
        result.Add(new("HOT SECTION", hotsectionSub, hotSectionHealth));

        // COMPRESSOR — average (1 - wear) for Compressor category, with wash-due indicator.
        var compressorHealth = AverageHealth(componentsByCategory.GetValueOrDefault(ComponentCategory.Compressor) ?? new());
        var compressorSub = compressorHealth < 0.4 ? "wash due" : "clean";
        result.Add(new("COMPRESSOR", compressorSub, compressorHealth));

        // OIL SYSTEM — based on Oil consumable level + OilSystem components.
        var oilLevel = consumablesByKind.GetValueOrDefault(ConsumableKind.Oil)?.FirstOrDefault()?.Level ?? 1.0;
        var oilCapacity = consumablesByKind.GetValueOrDefault(ConsumableKind.Oil)?.FirstOrDefault()?.Capacity ?? 14.0;
        var oilSystemComponents = componentsByCategory.GetValueOrDefault(ComponentCategory.OilSystem) ?? new();
        var oilComponentHealth = AverageHealth(oilSystemComponents);
        var oilHealth = (oilLevel + oilComponentHealth) / 2; // blend consumable + component health
        var oilSub = $"{(oilLevel * oilCapacity):F1} / {oilCapacity:F1} qt";
        result.Add(new("OIL SYSTEM", oilSub, oilHealth));

        // FUEL — FuelSystem components average.
        var fuelHealth = AverageHealth(componentsByCategory.GetValueOrDefault(ComponentCategory.FuelSystem) ?? new());
        var fuelSub = "clean · last sample 4.2 h ago";
        result.Add(new("FUEL", fuelSub, fuelHealth));

        // PROPELLER — Propeller category average.
        var propellerHealth = AverageHealth(componentsByCategory.GetValueOrDefault(ComponentCategory.Propeller) ?? new());
        result.Add(new("PROPELLER", "erosion + governor drift", propellerHealth));

        // GEAR · BRAKES — combine GearBrakes components + Tires/BrakePad consumables.
        var gearBrakesComponents = componentsByCategory.GetValueOrDefault(ComponentCategory.GearBrakes) ?? new();
        var tiresConsumable = consumablesByKind.GetValueOrDefault(ConsumableKind.Tire)?.FirstOrDefault();
        var brakePadConsumable = consumablesByKind.GetValueOrDefault(ConsumableKind.BrakePad)?.FirstOrDefault();
        var gearBrakesHealth = AverageHealth(gearBrakesComponents);
        var tiresHealth = tiresConsumable?.Level ?? 0.7;
        var brakesHealth = brakePadConsumable?.Level ?? 0.65;
        var gearBrakesBlended = (gearBrakesHealth + tiresHealth + brakesHealth) / 3;
        var gearBrakesSub = $"brakes {((int)Math.Round(brakesHealth * 100))}% · tires {((int)Math.Round(tiresHealth * 100))}%";
        result.Add(new("GEAR · BRAKES", gearBrakesSub, gearBrakesBlended));

        // ELECTRICAL — BatterySoh consumable level.
        var batteryConsumable = consumablesByKind.GetValueOrDefault(ConsumableKind.BatterySoh)?.FirstOrDefault();
        var electricalHealth = batteryConsumable?.Level ?? 0.91;
        var batterySoh = (int)Math.Round(electricalHealth * 100);
        result.Add(new("ELECTRICAL", $"battery SOH {batterySoh}%", electricalHealth));

        return result;
    }

    private double AverageHealth(IReadOnlyList<DataEntities.Component> components)
    {
        if (components.Count == 0)
            return 0.5; // TODO: stub value for no data.

        double sum = 0;
        foreach (var c in components)
            sum += (1 - Math.Max(0, Math.Min(1, c.Wear)));

        return sum / components.Count;
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        _healthRefreshTimer?.Dispose();
        _healthCts?.Cancel();
        _healthCts?.Dispose();
        _sampleSub.Dispose();
        _statusSub.Dispose();
        _airframeProvider.ActiveAirframeChanged -= OnAirframeChanged;
    }
}

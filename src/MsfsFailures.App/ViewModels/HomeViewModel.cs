using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsfsFailures.App.Services;

namespace MsfsFailures.App.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IFleetSource _fleet;

    public HomeViewModel(IFleetSource fleet)
    {
        _fleet = fleet;
        Airframes = new ObservableCollection<AirframeVm>(_fleet.GetAirframes());
        Squawks = new ObservableCollection<SquawkVm>(_fleet.GetSquawks());
        SelectedAirframe = Airframes.FirstOrDefault(a => a.Tail == "N350KA") ?? Airframes.First();
    }

    public ObservableCollection<AirframeVm> Airframes { get; }
    public ObservableCollection<SquawkVm> Squawks { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSquawks))]
    [NotifyPropertyChangedFor(nameof(SelectedSquawksCount))]
    [NotifyPropertyChangedFor(nameof(HasSelectedSquawks))]
    private AirframeVm _selectedAirframe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFleet))]
    [NotifyPropertyChangedFor(nameof(IsSquawks))]
    [NotifyPropertyChangedFor(nameof(IsMaintenance))]
    [NotifyPropertyChangedFor(nameof(IsSessions))]
    [NotifyPropertyChangedFor(nameof(IsEditor))]
    [NotifyPropertyChangedFor(nameof(IsBindings))]
    [NotifyPropertyChangedFor(nameof(IsLog))]
    private HomeTab _activeTab = HomeTab.Fleet;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SimChipText))]
    [NotifyPropertyChangedFor(nameof(LiveAircraft))]
    [NotifyPropertyChangedFor(nameof(LiveStripVisible))]
    private bool _simOk = true;

    public bool IsFleet       => ActiveTab == HomeTab.Fleet;
    public bool IsSquawks     => ActiveTab == HomeTab.Squawks;
    public bool IsMaintenance => ActiveTab == HomeTab.Maintenance;
    public bool IsSessions    => ActiveTab == HomeTab.Sessions;
    public bool IsEditor      => ActiveTab == HomeTab.AirframeEditor;
    public bool IsBindings    => ActiveTab == HomeTab.Bindings;
    public bool IsLog         => ActiveTab == HomeTab.Log;

    public AirframeVm? LiveAircraft => Airframes.FirstOrDefault(a => a.Live);
    public bool LiveStripVisible => SimOk && LiveAircraft != null;

    public IEnumerable<KeyValuePair<string,string>> LiveStripPairs
    {
        get
        {
            var ls = LiveAircraft?.LiveState;
            if (ls == null) yield break;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            yield return new("ALT",   ls.Altitude.ToString("N0", inv) + " ft");
            yield return new("IAS",   ls.Ias + " kt");
            yield return new("GS",    ls.Gs + " kt");
            yield return new("HDG",   ls.Hdg.ToString("D3", inv) + "°");
            yield return new("OAT",   ls.Oat + "°C");
            yield return new("N1",    string.Join("/", ls.N1.Select(v => v.ToString("F0", inv))) + "%");
            yield return new("ITT",   string.Join("/", ls.Itt) + "°C");
            yield return new("FUEL",  ls.FuelKg + "kg");
            yield return new("HOBBS+", "+" + ls.HobbsDelta.ToString("F1", inv));
        }
    }

    public string SimChipText => SimOk ? "SIMCONNECT●WASM●" : "SIM●OFFLINE";

    public IEnumerable<SquawkVm> SelectedSquawks =>
        Squawks.Where(s => s.Tail == SelectedAirframe.Tail);

    public int SelectedSquawksCount => SelectedSquawks.Count();
    public bool HasSelectedSquawks => SelectedSquawksCount > 0;

    // Tab counts
    public int FleetCount       => Airframes.Count;
    public int OpenSquawksCount => Squawks.Count(s => s.Severity != SquawkSeverity.Deferred);
    public int MxDueCount       => Airframes.Count(a => a.NextInspectionHrs < 10);

    // Status bar
    public int TotalAirframes => Airframes.Count;
    public int TotalSquawks   => Squawks.Count;

    [RelayCommand]
    private void ToggleSim() => SimOk = !SimOk;

    [RelayCommand]
    private void SelectTab(HomeTab tab) => ActiveTab = tab;

    [RelayCommand]
    private void SelectAirframe(AirframeVm a) => SelectedAirframe = a;

    // Stub action commands — wire to real services in a later pass.
    [RelayCommand] private void NewSquawk() { /* todo */ }
    [RelayCommand] private void DeferSquawk() { /* todo */ }
    [RelayCommand] private void ServiceComponent() { /* todo */ }
    [RelayCommand] private void OpenAirframe() { /* todo */ }
    [RelayCommand] private void ResetCycle() { /* todo */ }
    [RelayCommand] private void GroundAircraft() { /* todo */ }
}

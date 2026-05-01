using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.App.Services;
using MsfsFailures.Data.Repositories;

namespace MsfsFailures.App.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IFleetSource _fleet;
    private readonly IServiceScopeFactory? _scopeFactory;

    public HomeViewModel(IFleetSource fleet, InFlightViewModel inFlight,
                         IServiceScopeFactory scopeFactory)
    {
        _fleet       = fleet;
        _scopeFactory = scopeFactory;
        InFlight     = inFlight;
        Airframes    = new ObservableCollection<AirframeVm>(_fleet.GetAirframes());
        Squawks      = new ObservableCollection<SquawkVm>(_fleet.GetSquawks());
        SelectedAirframe = Airframes.FirstOrDefault(a => a.Tail == "N350KA") ?? Airframes.First();
    }

    public ObservableCollection<AirframeVm> Airframes { get; }
    public ObservableCollection<SquawkVm> Squawks { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSquawks))]
    [NotifyPropertyChangedFor(nameof(SelectedSquawksCount))]
    [NotifyPropertyChangedFor(nameof(HasSelectedSquawks))]
    [NotifyPropertyChangedFor(nameof(HasOpenSquawkForSelected))]
    private AirframeVm _selectedAirframe;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFleet))]
    [NotifyPropertyChangedFor(nameof(IsInFlight))]
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

    /// <summary>Non-null when the editor panel is open.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private SquawkVm? _editingSquawk;

    /// <summary>True when a new (unsaved) squawk is being created.</summary>
    [ObservableProperty]
    private bool _isNewSquawk;

    public bool IsEditing => EditingSquawk != null;

    public bool IsFleet       => ActiveTab == HomeTab.Fleet;
    public bool IsInFlight    => ActiveTab == HomeTab.InFlight;
    public bool IsSquawks     => ActiveTab == HomeTab.Squawks;
    public bool IsMaintenance => ActiveTab == HomeTab.Maintenance;
    public bool IsSessions    => ActiveTab == HomeTab.Sessions;
    public bool IsEditor      => ActiveTab == HomeTab.AirframeEditor;
    public bool IsBindings    => ActiveTab == HomeTab.Bindings;
    public bool IsLog         => ActiveTab == HomeTab.Log;

    public InFlightViewModel InFlight { get; }

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
    public bool HasSelectedSquawks  => SelectedSquawksCount > 0;

    /// <summary>True when the selected airframe has at least one Open squawk (enables DEFER button).</summary>
    public bool HasOpenSquawkForSelected =>
        SelectedSquawks.Any(s => s.Severity == SquawkSeverity.Open);

    // Tab counts
    public int FleetCount       => Airframes.Count;
    public int OpenSquawksCount => Squawks.Count(s => s.Severity != SquawkSeverity.Deferred);
    public int MxDueCount       => Airframes.Count(a => a.NextInspectionHrs < 10);

    // Status bar
    public int TotalAirframes => Airframes.Count;
    public int TotalSquawks   => Squawks.Count;

    // ── Basic navigation commands ────────────────────────────────────────────

    [RelayCommand]
    private void ToggleSim() => SimOk = !SimOk;

    [RelayCommand]
    private void SelectTab(HomeTab tab) => ActiveTab = tab;

    [RelayCommand]
    private void SelectAirframe(AirframeVm a) => SelectedAirframe = a;

    // ── Squawk commands ──────────────────────────────────────────────────────

    /// <summary>Opens the edit panel for a brand-new squawk against the selected airframe.</summary>
    [RelayCommand]
    private void NewSquawk()
    {
        EditingSquawk = new SquawkVm
        {
            SquawkGuid  = Guid.Empty,   // signals "not yet saved"
            Id          = "NEW",
            Tail        = SelectedAirframe.Tail,
            Opened      = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            HoursAtOpen = SelectedAirframe.Hours,
            Component   = string.Empty,
            Summary     = string.Empty,
            Notes       = string.Empty,
            Severity    = SquawkSeverity.Open,
            MelDeferrable = false,
        };
        IsNewSquawk = true;
        ActiveTab   = HomeTab.Squawks;
    }

    /// <summary>Opens the edit panel for an existing squawk.</summary>
    [RelayCommand]
    private void EditSquawk(SquawkVm squawk)
    {
        EditingSquawk = squawk;
        IsNewSquawk   = false;
    }

    /// <summary>Cancels the current edit and closes the panel.</summary>
    [RelayCommand]
    private void CancelEdit()
    {
        EditingSquawk = null;
        IsNewSquawk   = false;
    }

    /// <summary>Saves current edits — creates or updates via repository.</summary>
    [RelayCommand]
    private async Task SaveSquawkAsync()
    {
        var sq = EditingSquawk;
        if (sq == null) return;

        if (_scopeFactory == null) { EditingSquawk = null; return; }

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();

        if (IsNewSquawk)
        {
            // Resolve the airframe Guid from the Tail.
            var all = await repo.GetAllAirframesAsync();
            var af  = all.FirstOrDefault(a => a.Tail == sq.Tail);
            if (af != null)
            {
                var newId = await repo.AddSquawkAsync(
                    af.Id,
                    sq.Component,
                    sq.Summary,
                    (int)sq.Severity,
                    sq.MelDeferrable,
                    sq.Notes,
                    sq.HoursAtOpen);
                _ = newId; // used by DB; VM refreshed below
            }
        }
        else
        {
            if (sq.SquawkGuid != Guid.Empty)
            {
                await repo.UpdateSquawkAsync(
                    sq.SquawkGuid,
                    sq.Component,
                    sq.Summary,
                    sq.Notes,
                    (int)sq.Severity,
                    null);
            }
        }

        EditingSquawk = null;
        IsNewSquawk   = false;
        await ReloadSquawksAsync();
    }

    /// <summary>Defers the supplied squawk (status → Deferred, DeferredUntil = now+10 days).</summary>
    [RelayCommand]
    private async Task DeferSquawkAsync(SquawkVm squawk)
    {
        if (_scopeFactory == null) return;

        squawk.Severity     = SquawkSeverity.Deferred;
        squawk.DeferredUntil = DateTimeOffset.UtcNow.AddDays(10).ToString("yyyy-MM-dd");

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
        if (squawk.SquawkGuid != Guid.Empty)
        {
            await repo.UpdateSquawkAsync(
                squawk.SquawkGuid,
                component: null,
                summary: null,
                notes: null,
                status: 1, // SquawkStatus.Deferred
                deferredUntil: DateTimeOffset.UtcNow.AddDays(10));
        }

        OnPropertyChanged(nameof(SelectedSquawks));
        OnPropertyChanged(nameof(HasOpenSquawkForSelected));
    }

    /// <summary>Defers the most recent open squawk for the selected airframe (from DetailAside button).</summary>
    [RelayCommand]
    private async Task DeferLatestSquawkAsync()
    {
        var open = SelectedSquawks.FirstOrDefault(s => s.Severity == SquawkSeverity.Open);
        if (open == null) return;
        await DeferSquawkAsync(open);
    }

    /// <summary>Closes a squawk (status → Closed).</summary>
    [RelayCommand]
    private async Task CloseSquawkAsync(SquawkVm squawk)
    {
        squawk.Severity = SquawkSeverity.Deferred; // reuse Deferred style; Closed not in enum — map to Deferred visually

        if (_scopeFactory == null) return;
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
        if (squawk.SquawkGuid != Guid.Empty)
        {
            await repo.UpdateSquawkAsync(
                squawk.SquawkGuid,
                component: null,
                summary: null,
                notes: null,
                status: 2, // SquawkStatus.Closed
                deferredUntil: null);
        }

        // Remove from the live collection so it disappears from the table.
        Squawks.Remove(squawk);
        OnPropertyChanged(nameof(SelectedSquawks));
        OnPropertyChanged(nameof(SelectedSquawksCount));
        OnPropertyChanged(nameof(HasSelectedSquawks));
        OnPropertyChanged(nameof(HasOpenSquawkForSelected));
    }

    // ── Stub action commands (other buttons) ─────────────────────────────────

    [RelayCommand] private void ServiceComponent() { /* todo */ }
    [RelayCommand] private void OpenAirframe() { /* todo */ }
    [RelayCommand] private void ResetCycle() { /* todo */ }
    [RelayCommand] private void GroundAircraft() { /* todo */ }

    // ── Reload helpers ───────────────────────────────────────────────────────

    /// <summary>Re-pulls all squawks from the repository and refreshes the collection on the UI thread.</summary>
    private async Task ReloadSquawksAsync()
    {
        if (_scopeFactory == null) return;

        List<SquawkVm> fresh;
        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
            var entities = await repo.GetAllSquawksAsync();
            fresh = entities.Select(RepositoryFleetSource.MapSquawk).ToList();
        }

        // Mutate on the calling thread (always the UI thread for commands).
        Squawks.Clear();
        foreach (var vm in fresh)
            Squawks.Add(vm);

        OnPropertyChanged(nameof(SelectedSquawks));
        OnPropertyChanged(nameof(SelectedSquawksCount));
        OnPropertyChanged(nameof(HasSelectedSquawks));
        OnPropertyChanged(nameof(HasOpenSquawkForSelected));
        OnPropertyChanged(nameof(OpenSquawksCount));
        OnPropertyChanged(nameof(TotalSquawks));
    }
}

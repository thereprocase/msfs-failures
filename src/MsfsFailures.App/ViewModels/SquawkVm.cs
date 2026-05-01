using CommunityToolkit.Mvvm.ComponentModel;

namespace MsfsFailures.App.ViewModels;

/// <summary>
/// Observable VM for a single squawk. Backing fields are writable so the
/// edit panel can two-way bind without a separate DTO.
/// </summary>
public sealed partial class SquawkVm : ObservableObject
{
    // The full database Guid — used for persistence calls.
    public Guid SquawkGuid { get; init; }

    // Short display ID (first 8 hex chars, upper-case).
    public string Id { get; init; } = string.Empty;

    // Read-only after creation.
    public string Tail { get; init; } = string.Empty;
    public string Opened { get; init; } = string.Empty;
    public double HoursAtOpen { get; init; }

    // Editable fields — backed by [ObservableProperty].
    [ObservableProperty] private string _component = string.Empty;
    [ObservableProperty] private string _summary = string.Empty;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private SquawkSeverity _severity;
    [ObservableProperty] private bool _melDeferrable;
    [ObservableProperty] private string? _deferredUntil;

    // Display helpers (derived — no INotifyPropertyChanged needed).
    public string SeverityLabel => Severity.ToString().ToUpperInvariant();
    public string MelLabel => MelDeferrable ? "YES" : "NO";
}

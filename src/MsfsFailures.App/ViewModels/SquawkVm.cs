namespace MsfsFailures.App.ViewModels;

public sealed class SquawkVm
{
    public required string Id { get; init; }
    public required string Tail { get; init; }
    public required string Component { get; init; }
    public required string Summary { get; init; }
    public required SquawkSeverity Severity { get; init; }
    public required bool MelDeferrable { get; init; }
    public required string Opened { get; init; }
    public required double HoursAtOpen { get; init; }
    public string? DeferredUntil { get; init; }
    public required string Notes { get; init; }

    public string SeverityLabel => Severity.ToString().ToUpperInvariant();
    public string MelLabel => MelDeferrable ? "YES" : "NO";
}

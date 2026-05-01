namespace MsfsFailures.Core.Failure;

/// <summary>
/// The result of one tick roll across all components.
/// </summary>
/// <param name="Triggered">Failures that fired this tick.</param>
/// <param name="Diagnostics">Human-readable diagnostic strings (missing templates, degenerate MTBF, etc.).</param>
public sealed record FailureRollResult(
    IReadOnlyList<TriggeredFailure> Triggered,
    IReadOnlyList<string> Diagnostics);

/// <summary>
/// One failure that triggered during a tick roll.
/// </summary>
/// <param name="ComponentId">Id of the Component instance that failed.</param>
/// <param name="FailureModeId">Id of the FailureMode that was triggered.</param>
/// <param name="ReasonShort">
/// Human-readable summary of the roll parameters, e.g.:
/// "weibull β=1.58 α=2000h effMtbf=1240h wearFactor=1.80 multiplier=1.50 pTick=0.000042"
/// </param>
public sealed record TriggeredFailure(
    Guid ComponentId,
    Guid FailureModeId,
    string ReasonShort);

namespace MsfsFailures.Core.Failure;

/// <summary>
/// Per-tick Poisson failure roller.
/// The caller (tick loop) is responsible for filtering out components that already
/// have open squawks of <see cref="FailureSeverity.Grounding"/> severity — those
/// components should not be passed in, or their failure modes should be excluded
/// from the <paramref name="failureModes"/> list.
/// </summary>
public interface IFailureEngine
{
    /// <summary>
    /// Performs one Poisson dice roll per component per failure mode.
    /// </summary>
    /// <param name="airframe">Airframe context (used for diagnostics/logging only in v1).</param>
    /// <param name="components">All installed components to evaluate this tick.</param>
    /// <param name="templates">Full template catalogue; matched by <see cref="Component.TemplateId"/>.</param>
    /// <param name="failureModes">All failure modes; filtered by <see cref="FailureMode.TemplateId"/>.</param>
    /// <param name="accelerators">All active accelerators; filtered by category.</param>
    /// <param name="sampleVars">Current flight-state variable bag (e.g. "oil_temp_C" → 118.5).</param>
    /// <param name="dt">
    /// Time step for this tick. 4 Hz tick loop → 250 ms typical.
    /// Smaller dt → smaller per-tick probability; the Poisson integral is exact.
    /// </param>
    FailureRollResult Roll(
        Airframe airframe,
        IReadOnlyList<Component> components,
        IReadOnlyList<ComponentTemplate> templates,
        IReadOnlyList<FailureMode> failureModes,
        IReadOnlyList<Accelerator> accelerators,
        IReadOnlyDictionary<string, double> sampleVars,
        TimeSpan dt);
}

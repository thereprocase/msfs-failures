namespace MsfsFailures.Core.Wear;

/// <summary>
/// Computes per-tick wear, consumable consumption, and event notes for one airframe.
/// Implementations must be pure functions: no I/O, no logging, no side effects.
/// </summary>
public interface IWearEngine
{
    /// <summary>
    /// Apply one simulation tick (typically 250 ms at 4 Hz).
    /// </summary>
    /// <param name="airframe">The airframe being evaluated.</param>
    /// <param name="components">All components installed on this airframe, with their current templates resolved.</param>
    /// <param name="consumables">All consumables tracked for this airframe.</param>
    /// <param name="accelerators">Active accelerator rules to evaluate against the sample.</param>
    /// <param name="sample">Flight state snapshot for this tick.</param>
    /// <param name="dt">Elapsed time since the previous tick.</param>
    /// <returns>Deltas to apply; never null.</returns>
    WearTickResult Tick(
        Airframe airframe,
        IReadOnlyList<Component> components,
        IReadOnlyList<Consumable> consumables,
        IReadOnlyList<Accelerator> accelerators,
        FlightTickSample sample,
        TimeSpan dt);
}

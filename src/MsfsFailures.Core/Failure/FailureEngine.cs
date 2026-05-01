namespace MsfsFailures.Core.Failure;

/// <summary>
/// Concrete Poisson-based failure roller.
///
/// ## Algorithm per component
///
/// 1. Resolve the ComponentTemplate (skip with diagnostic if missing).
///
/// 2. Compute effective MTBF:
///    a. Read Weibull β and α from WearCurveJson; fall back to WeibullDefaults by category.
///    b. Compute the accelerator multiplier (product of all matching accelerators that fire).
///    c. Apply the Weibull hazard-rate correction:
///         effectiveMtbf = (α / multiplier^(1/β)) * wearFactor
///       where wearFactor = 1 + component.Wear * 2.
///       Rationale: wear [0..1] linearly scales effective life from 1× (new) down to 1/3×
///       at full wear (wearFactor denominator 3). Multiplying by (1 + wear*2) shortens MTBF
///       as wear increases. This is a heuristic informed by the C208B mod's "each overstress
///       second decrements MTTF" idiom (Pattern 3, reference/aircraft-systems/summary.md).
///       It is not derived from first principles; document accordingly.
///
///    Note: the Weibull hazard rate at time t is h(t) = (β/α)(t/α)^(β−1).
///    For simplicity in v1, we do NOT track exact component age for instantaneous hazard;
///    instead we use the mean of the Weibull distribution (α·Γ(1+1/β)) as the base MTBF
///    and let the accelerator multiplier + wear heuristic substitute for the age effect.
///    This is documented as a known approximation. A future version could compute
///    h(component.Hours) directly for true Weibull aging.
///
/// 3. Build the failure-mode pool for this template.
///    Per-mode MTBF = effectiveMtbf * modeCount   (equal budget split — v1 assumption;
///    a future version could weight by mode-specific severity or sub-MTBF fields).
///
/// 4. Per mode, compute pTick = 1 − exp(−dt / effectiveMtbfPerMode) capped at 0.999.
///    Roll a uniform (0,1); if &lt; pTick, add to Triggered list.
///
/// ## Thread safety
/// Not thread-safe. One instance per tick loop thread is expected.
/// </summary>
public sealed class FailureEngine : IFailureEngine
{
    private readonly Random _rng;

    /// <param name="seed">
    /// RNG seed. 0 = use <see cref="Random.Shared"/> entropy (non-deterministic).
    /// Any other value produces a deterministic sequence for testing.
    /// </param>
    public FailureEngine(int seed = 0)
    {
        _rng = seed == 0 ? new Random() : new Random(seed);
    }

    /// <inheritdoc/>
    public FailureRollResult Roll(
        Airframe airframe,
        IReadOnlyList<Component> components,
        IReadOnlyList<ComponentTemplate> templates,
        IReadOnlyList<FailureMode> failureModes,
        IReadOnlyList<Accelerator> accelerators,
        IReadOnlyDictionary<string, double> sampleVars,
        TimeSpan dt)
    {
        var triggered   = new List<TriggeredFailure>();
        var diagnostics = new List<string>();

        // Build lookup maps once per call
        var templateById = BuildLookup(templates, t => t.Id);
        var modesByTemplate = BuildGrouping(failureModes, m => m.TemplateId);

        double dtHours = dt.TotalHours;

        foreach (var component in components)
        {
            // 1. Resolve template
            if (!templateById.TryGetValue(component.TemplateId, out var template))
            {
                diagnostics.Add(
                    $"Component {component.Id}: template {component.TemplateId} not found — skipped.");
                continue;
            }

            // Build failure-mode pool
            if (!modesByTemplate.TryGetValue(template.Id, out var modes) || modes.Count == 0)
            {
                diagnostics.Add(
                    $"Component {component.Id} ({template.Name}): no modes registered — skipped.");
                continue;
            }

            // 2a. Weibull parameters from WearCurve JSON, then defaults
            var (jsonBeta, jsonAlpha) = WearCurveJson.ReadWeibull(template.WearCurve);
            var (defaultBeta, defaultAlpha) = WeibullDefaults.For(template.Category);

            double beta  = jsonBeta  ?? defaultBeta;
            double alpha = jsonAlpha ?? defaultAlpha;

            // Use template.MtbfHours as alpha override if no alpha in WearCurve and
            // MtbfHours has been explicitly set (non-zero).
            if (jsonAlpha is null && template.MtbfHours > 0)
                alpha = template.MtbfHours;

            // 2b. Accelerator multiplier (≥ 1.0)
            double multiplier = AcceleratorEvaluator.ComputeMultiplier(
                accelerators, template.Category, sampleVars);

            // 2c. Effective MTBF
            // Weibull alpha / multiplier^(1/beta) gives the corrected characteristic life
            // under the accelerated stress.  Then the wear heuristic further shortens it.
            double wearFactor = 1.0 + component.Wear * 2.0;   // [1.0 at wear=0 … 3.0 at wear=1]
            double effectiveMtbf;

            if (beta <= 0 || alpha <= 0)
            {
                effectiveMtbf = 0;
            }
            else
            {
                // Corrected alpha under multiplier pressure
                double correctedAlpha = alpha / Math.Pow(multiplier, 1.0 / beta);
                // Shorten further by wear (higher wear → smaller effectiveMtbf)
                effectiveMtbf = correctedAlpha / wearFactor;
            }

            if (effectiveMtbf <= 0)
            {
                diagnostics.Add(
                    $"Component {component.Id} ({template.Name}): effectiveMtbf ≤ 0 " +
                    $"(β={beta:F2} α={alpha:F0}h mult={multiplier:F3} wear={component.Wear:F2}) " +
                    "— treating as immediate failure.");

                // Force all modes to trigger
                foreach (var mode in modes)
                {
                    triggered.Add(new TriggeredFailure(
                        component.Id, mode.Id,
                        $"effectiveMtbf≤0 β={beta:F2} α={alpha:F0}h mult={multiplier:F3} " +
                        $"wear={component.Wear:F2} — immediate"));
                }
                continue;
            }

            // 3. Per-mode MTBF (equal budget split)
            int modeCount = modes.Count;
            double effectiveMtbfPerMode = effectiveMtbf * modeCount;

            // 4. Roll each mode
            foreach (var mode in modes)
            {
                double pTick = 1.0 - Math.Exp(-dtHours / effectiveMtbfPerMode);
                pTick = Math.Min(pTick, 0.999);

                double roll = _rng.NextDouble();   // (0.0, 1.0)
                if (roll < pTick)
                {
                    string reason =
                        $"weibull β={beta:F2} α={alpha:F0}h effMtbf={effectiveMtbf:F0}h " +
                        $"modes={modeCount} effMtbfPerMode={effectiveMtbfPerMode:F0}h " +
                        $"wearFactor={wearFactor:F2} multiplier={multiplier:F3} " +
                        $"pTick={pTick:G4} roll={roll:G4}";

                    triggered.Add(new TriggeredFailure(component.Id, mode.Id, reason));
                }
            }
        }

        return new FailureRollResult(triggered, diagnostics);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static Dictionary<TKey, TValue> BuildLookup<TKey, TValue>(
        IReadOnlyList<TValue> source,
        Func<TValue, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, TValue>(source.Count);
        foreach (var item in source)
        {
            var key = keySelector(item);
            dict.TryAdd(key, item);   // first wins if duplicates
        }
        return dict;
    }

    private static Dictionary<TKey, List<TValue>> BuildGrouping<TKey, TValue>(
        IReadOnlyList<TValue> source,
        Func<TValue, TKey> keySelector)
        where TKey : notnull
    {
        var dict = new Dictionary<TKey, List<TValue>>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<TValue>();
                dict[key] = list;
            }
            list.Add(item);
        }
        return dict;
    }
}

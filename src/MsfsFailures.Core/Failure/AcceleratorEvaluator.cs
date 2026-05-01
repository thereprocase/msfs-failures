using System.Text.Json;

namespace MsfsFailures.Core.Failure;

/// <summary>
/// Evaluates a list of Accelerators against the current sample variables to produce
/// a combined multiplier for effective MTBF reduction.
///
/// Formula JSON shape (same as WearEngine; FailureEngine only reads the multiplier field
/// or falls back to a simple threshold/scale parse):
/// {
///   "multiplier": 1.5               // flat override — use as-is
/// }
/// OR
/// {
///   "type": "linear",
///   "threshold": 115.0,
///   "scale": 1.5                   // multiplier when var > threshold
/// }
///
/// For v1, FailureEngine uses a simplified evaluation:
///   - If the formula has a "multiplier" key, use that value unconditionally.
///   - If the variable exists in sampleVars and exceeds "threshold", apply "scale".
///   - Otherwise multiplier = 1.0 (no effect).
///
/// The product of all applicable accelerator multipliers is returned.
/// </summary>
internal static class AcceleratorEvaluator
{
    /// <summary>
    /// Returns the product of all accelerator multipliers that fire for the given category.
    /// Result is always ≥ 1.0.
    /// </summary>
    public static double ComputeMultiplier(
        IReadOnlyList<Accelerator> accelerators,
        ComponentCategory category,
        IReadOnlyDictionary<string, double> sampleVars)
    {
        double product = 1.0;

        foreach (var acc in accelerators)
        {
            if (acc.Category != category)
                continue;

            double m = EvaluateOne(acc, sampleVars);
            if (m > 1.0)
                product *= m;
        }

        return product;
    }

    private static double EvaluateOne(Accelerator acc, IReadOnlyDictionary<string, double> sampleVars)
    {
        if (string.IsNullOrWhiteSpace(acc.Formula))
            return 1.0;

        try
        {
            using var doc = JsonDocument.Parse(acc.Formula);
            var root = doc.RootElement;

            // Flat multiplier override
            if (root.TryGetProperty("multiplier", out var mProp)
                && mProp.TryGetDouble(out double flatM))
                return Math.Max(1.0, flatM);

            // Threshold/scale form
            if (root.TryGetProperty("threshold", out var thProp)
                && thProp.TryGetDouble(out double threshold)
                && root.TryGetProperty("scale", out var scProp)
                && scProp.TryGetDouble(out double scale))
            {
                if (sampleVars.TryGetValue(acc.Variable, out double varValue)
                    && varValue > threshold)
                    return Math.Max(1.0, scale);
            }
        }
        catch (JsonException)
        {
            // Malformed formula — skip silently; FailureEngine will emit no diagnostic
            // for evaluator-internal issues (formula authoring is out of scope here).
        }

        return 1.0;
    }
}

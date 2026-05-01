namespace MsfsFailures.Core.Failure;

/// <summary>
/// Default Weibull shape (β) and scale (α, hours) parameters by component category.
/// Sourced from NASA/CR-2001-210647 GA reliability field data and the recommended
/// defaults table in reference/reliability/summary.md.
///
/// β &lt; 1  → infant mortality / decreasing hazard
/// β = 1  → constant / random hazard (exponential)
/// β &gt; 1  → wear-out (increasing hazard)
/// </summary>
internal static class WeibullDefaults
{
    // (beta, alpha_hours) pairs
    private static readonly Dictionary<ComponentCategory, (double Beta, double Alpha)> Defaults = new()
    {
        // Engine (piston): NASA Table 9 scaled to GA TBO ~2,000 hr
        [ComponentCategory.Engine]     = (1.58, 2000.0),

        // Hot section / turbine: wear-out, tighter band
        [ComponentCategory.HotSection] = (1.80, 3500.0),

        // Compressor: near-random/moderate wear
        [ComponentCategory.Compressor] = (1.60, 4000.0),

        // Oil system: near-random, seal degradation
        [ComponentCategory.OilSystem]  = (1.14, 3977.0),

        // Fuel system: NASA Table 9
        [ComponentCategory.FuelSystem] = (1.44, 5130.0),

        // Propeller: NASA Table 9; prop overhaul ~2,400 hr
        [ComponentCategory.Propeller]  = (1.63, 2400.0),

        // Landing gear / brakes: near-random; shortest life (NASA gear = 0.92)
        [ComponentCategory.GearBrakes] = (0.92, 1500.0),

        // Tires: cycle-based; near-random
        [ComponentCategory.Tires]      = (1.50, 300.0),

        // Battery: capacity decay; slight infant mortality territory
        [ComponentCategory.Battery]    = (0.90, 4000.0),

        // Hydraulics: seal degradation, near-random
        [ComponentCategory.Hydraulic]  = (1.14, 3977.0),

        // Avionics / electrics: NASA lighting/distribution data
        [ComponentCategory.Avionics]   = (1.67, 4950.0),

        // Other: pure random / no data
        [ComponentCategory.Other]      = (1.00, 5000.0),
    };

    /// <summary>
    /// Returns the (beta, alpha) pair for the given category.
    /// Falls back to β=1.0, α=5000 hr if category is not in the table.
    /// </summary>
    public static (double Beta, double Alpha) For(ComponentCategory category)
        => Defaults.TryGetValue(category, out var v) ? v : (1.0, 5000.0);
}

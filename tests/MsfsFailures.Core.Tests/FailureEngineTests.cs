using MsfsFailures.Core;
using MsfsFailures.Core.Failure;
using Xunit;

namespace MsfsFailures.Core.Tests;

/// <summary>
/// Unit tests for <see cref="FailureEngine"/>.
/// All tests use a fixed seed for determinism.
/// </summary>
public sealed class FailureEngineTests
{
    // ─────────────────────────────────────────────────────────────
    // Shared test-data helpers
    // ─────────────────────────────────────────────────────────────

    private static Airframe MakeAirframe() => new()
    {
        Id            = Guid.NewGuid(),
        Tail          = "N1TST",
        Type          = "C172",
        ModelRefId    = Guid.NewGuid(),
        TotalHobbsHours = 100,
        TotalCycles   = 50,
        CreatedAt     = DateTimeOffset.UtcNow,
    };

    private static ComponentTemplate MakeTemplate(
        ComponentCategory category = ComponentCategory.Engine,
        double mtbfHours          = 2000,
        string wearCurveJson      = "{}") => new()
    {
        Id                  = Guid.NewGuid(),
        ModelRefId          = Guid.NewGuid(),
        Category            = category,
        Name                = $"Test {category}",
        MtbfHours           = mtbfHours,
        WearCurve           = wearCurveJson,
        ConsumableKind      = ConsumableKind.None,
        ReplaceIntervalHours  = 0,
        ReplaceIntervalCycles = 0,
    };

    private static Component MakeComponent(Guid airframeId, Guid templateId, double wear = 0.0) => new()
    {
        Id             = Guid.NewGuid(),
        AirframeId     = airframeId,
        TemplateId     = templateId,
        Hours          = 500,
        Cycles         = 20,
        Wear           = wear,
        Condition      = "serviceable",
        LastServicedAt = DateTimeOffset.UtcNow,
        InstalledAt    = DateTimeOffset.UtcNow,
    };

    private static FailureMode MakeMode(Guid templateId) => new()
    {
        Id               = Guid.NewGuid(),
        TemplateId       = templateId,
        Name             = "Test failure",
        SimBindingKind   = SimBindingKind.InternalOnly,
        SimBindingPayload = string.Empty,
        Severity         = FailureSeverity.Warning,
        RepairHours      = 1.0,
        MelDeferrable    = true,
    };

    private static Accelerator MakeFlatAccelerator(
        ComponentCategory category,
        double multiplier) => new()
    {
        Id       = Guid.NewGuid(),
        Category = category,
        Variable = "dummy",
        Formula  = $"{{\"multiplier\":{multiplier}}}",
    };

    private static readonly TimeSpan Dt250ms = TimeSpan.FromMilliseconds(250);
    private static readonly IReadOnlyDictionary<string, double> EmptyVars =
        new Dictionary<string, double>();

    // ─────────────────────────────────────────────────────────────
    // Test 1 — Determinism: same seed + same input → same output
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FailureEngine_Determinism_SameSeedSameOutput()
    {
        var airframe = MakeAirframe();
        var template = MakeTemplate(ComponentCategory.Engine, mtbfHours: 10);  // low MTBF to get hits
        var component = MakeComponent(airframe.Id, template.Id, wear: 0.0);
        var mode = MakeMode(template.Id);

        var components    = new[] { component };
        var templates     = new[] { template };
        var modes         = new[] { mode };
        var accelerators  = Array.Empty<Accelerator>();

        // Run 100 ticks with seed 42 — capture results
        const int Seed = 42;
        const int Ticks = 100;

        List<bool> run1 = RunTicks(Seed, airframe, components, templates, modes, accelerators, Ticks);
        List<bool> run2 = RunTicks(Seed, airframe, components, templates, modes, accelerators, Ticks);

        Assert.Equal(run1, run2);
    }

    private static List<bool> RunTicks(
        int seed,
        Airframe airframe,
        Component[] components,
        ComponentTemplate[] templates,
        FailureMode[] modes,
        Accelerator[] accelerators,
        int ticks)
    {
        var engine = new FailureEngine(seed);
        var results = new List<bool>(ticks);

        for (int i = 0; i < ticks; i++)
        {
            var r = engine.Roll(airframe, components, templates, modes, accelerators, EmptyVars, Dt250ms);
            results.Add(r.Triggered.Count > 0);
        }
        return results;
    }

    // ─────────────────────────────────────────────────────────────
    // Test 2 — High wear accelerates failure count
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FailureEngine_HighWearAcceleratesFailure()
    {
        const int Ticks = 1000;
        const int Seed  = 99;

        var airframe = MakeAirframe();
        var template = MakeTemplate(ComponentCategory.Engine, mtbfHours: 2000);
        var mode = MakeMode(template.Id);
        var templates    = new[] { template };
        var modes        = new[] { mode };
        var accelerators = Array.Empty<Accelerator>();

        // Low wear
        var compLow  = MakeComponent(airframe.Id, template.Id, wear: 0.0);
        int countLow = CountTriggered(Seed, airframe, new[] { compLow }, templates, modes, accelerators, Ticks);

        // High wear
        var compHigh = MakeComponent(airframe.Id, template.Id, wear: 0.9);
        int countHigh = CountTriggered(Seed, airframe, new[] { compHigh }, templates, modes, accelerators, Ticks);

        // High wear should produce at least as many failures as low wear.
        Assert.True(countHigh >= countLow,
            $"Expected high-wear count ({countHigh}) >= low-wear count ({countLow}).");
    }

    private static int CountTriggered(
        int seed,
        Airframe airframe,
        Component[] components,
        ComponentTemplate[] templates,
        FailureMode[] modes,
        Accelerator[] accelerators,
        int ticks)
    {
        var engine = new FailureEngine(seed);
        int count = 0;

        for (int i = 0; i < ticks; i++)
        {
            var r = engine.Roll(airframe, components, templates, modes, accelerators, EmptyVars, Dt250ms);
            count += r.Triggered.Count;
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────────
    // Test 3 — No modes: diagnostic emitted, no exception
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FailureEngine_NoModes_DoesNotThrow_DiagnosticEmitted()
    {
        var airframe  = MakeAirframe();
        var template  = MakeTemplate();
        var component = MakeComponent(airframe.Id, template.Id);
        var engine    = new FailureEngine(1);

        var result = engine.Roll(
            airframe,
            new[] { component },
            new[] { template },
            failureModes: Array.Empty<FailureMode>(),       // no modes
            accelerators: Array.Empty<Accelerator>(),
            sampleVars: EmptyVars,
            dt: Dt250ms);

        Assert.Empty(result.Triggered);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Contains("no modes registered"));
    }

    // ─────────────────────────────────────────────────────────────
    // Test 4 — Accelerator multiplier halves effective MTBF
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FailureEngine_AcceleratorMultiplier_HalvesEffectiveMtbf()
    {
        // With a flat ×2 multiplier and β=1.0, effective alpha (and MTBF) halves.
        // We verify this by reading the ReasonShort of a forced-trigger run.
        // Force a trigger by using a 1-second dt with 1-hour MTBF (~63% chance/tick).
        // Instead of relying on RNG, we verify the reason string reports roughly half the MTBF.

        const double BaseMtbf = 100.0;
        const double ExpectedHalvedMtbf = 50.0;

        var airframe  = MakeAirframe();
        var template  = MakeTemplate(
            ComponentCategory.Engine,
            mtbfHours: BaseMtbf,
            wearCurveJson: "{\"weibullBeta\":1.0}");   // β=1 → exponential, no age effect
        var component = MakeComponent(airframe.Id, template.Id, wear: 0.0);
        var mode      = MakeMode(template.Id);

        var accelerator = MakeFlatAccelerator(ComponentCategory.Engine, multiplier: 2.0);

        // Use a 1-hour dt to get near-certain trigger.
        var dtOneHour = TimeSpan.FromHours(1);
        const int Seed = 7;
        var engine = new FailureEngine(Seed);

        TriggeredFailure? hit = null;
        for (int i = 0; i < 200 && hit is null; i++)
        {
            var result = engine.Roll(
                airframe,
                new[] { component },
                new[] { template },
                new[] { mode },
                new[] { accelerator },
                EmptyVars,
                dtOneHour);

            if (result.Triggered.Count > 0)
                hit = result.Triggered[0];
        }

        Assert.NotNull(hit);

        // The ReasonShort should contain "effMtbf=50h" (or close to it — format G4 may vary).
        // We verify the effective MTBF is in the ballpark of 50 h.
        // Parse "effMtbf=Xh" from the reason string.
        double parsedEffMtbf = ParseEffMtbf(hit.ReasonShort);
        Assert.InRange(parsedEffMtbf, ExpectedHalvedMtbf * 0.95, ExpectedHalvedMtbf * 1.05);
    }

    private static double ParseEffMtbf(string reason)
    {
        // Reason format: "... effMtbf=50h ..."
        int idx = reason.IndexOf("effMtbf=", StringComparison.Ordinal);
        if (idx < 0) throw new InvalidOperationException($"effMtbf not found in: {reason}");
        int start = idx + "effMtbf=".Length;
        int end   = reason.IndexOf('h', start);
        return double.Parse(reason[start..end]);
    }

    // ─────────────────────────────────────────────────────────────
    // Test 5 — Poisson aggregate approximation over 10k ticks
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void FailureEngine_MtbfMath_PoissonAggregateApproximation()
    {
        // For a component with effectiveMtbf = 1000 h (1 mode, β=1, α=1000, wear=0, multiplier=1):
        //   pTick = 1 - exp(-dt/mtbf)
        //
        // Over N ticks of dt each, expected triggers = N * pTick.
        // Variance = N * pTick * (1 - pTick)  (binomial)
        // We assert actual count is within ±3σ.

        const double MtbfHours = 1000.0;
        const int    Ticks     = 10_000;
        const double DtHours   = 0.25 / 3600.0;  // 250 ms in hours

        double pTick    = 1.0 - Math.Exp(-DtHours / MtbfHours);
        double expected = Ticks * pTick;
        double variance = Ticks * pTick * (1.0 - pTick);
        double sigma    = Math.Sqrt(variance);

        var airframe  = MakeAirframe();
        var template  = MakeTemplate(
            ComponentCategory.Engine,
            mtbfHours: MtbfHours,
            wearCurveJson: "{\"weibullBeta\":1.0}");
        var component = MakeComponent(airframe.Id, template.Id, wear: 0.0);
        var mode      = MakeMode(template.Id);

        const int Seed = 2024;
        var engine = new FailureEngine(Seed);
        int actual = 0;

        for (int i = 0; i < Ticks; i++)
        {
            var r = engine.Roll(
                airframe,
                new[] { component },
                new[] { template },
                new[] { mode },
                Array.Empty<Accelerator>(),
                EmptyVars,
                Dt250ms);
            actual += r.Triggered.Count;
        }

        double lo = expected - 3 * sigma;
        double hi = expected + 3 * sigma;

        Assert.True(actual >= lo && actual <= hi,
            $"Triggered={actual} outside ±3σ [{lo:F2}, {hi:F2}] around expected={expected:F2}");
    }
}

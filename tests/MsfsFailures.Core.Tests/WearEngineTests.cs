using MsfsFailures.Core;
using MsfsFailures.Core.Wear;
using Xunit;

namespace MsfsFailures.Core.Tests;

/// <summary>
/// Unit tests for <see cref="WearEngine"/>.
///
/// All tests use fixed, deterministic inputs — WearEngine contains no RNG.
/// </summary>
public sealed class WearEngineTests
{
    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    private static Airframe MakeAirframe() => new()
    {
        Id = Guid.NewGuid(),
        Tail = "N1TEST",
        Type = "C172",
        ModelRefId = Guid.NewGuid(),
        TotalHobbsHours = 0,
        TotalCycles = 0,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Component MakeComponent(
        Guid airframeId,
        ComponentCategory category = ComponentCategory.Engine,
        double wear = 0.0) => new()
    {
        Id = Guid.NewGuid(),
        AirframeId = airframeId,
        TemplateId = Guid.NewGuid(),
        Hours = 0,
        Cycles = 0,
        Wear = wear,
        Condition = "serviceable",
        LastServicedAt = DateTimeOffset.UtcNow,
        InstalledAt = DateTimeOffset.UtcNow
    };

    private static Consumable MakeConsumable(
        Guid airframeId,
        ConsumableKind kind,
        double level = 1.0,
        double capacity = 8.0) => new()
    {
        Id = Guid.NewGuid(),
        AirframeId = airframeId,
        Kind = kind,
        Level = level,
        Capacity = capacity,
        LastTopUpAt = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Builds a nominal engine-running sample with controllable fields.
    /// </summary>
    private static FlightTickSample MakeEngineSample(
        double engineRpm = 2300,
        double n1Pct = 90,
        double oilTempC = 95,
        double gsKt = 0,
        bool onGround = false,
        double groundspeedAtTouchdownKt = 0,
        double brakeEnergyJoules = 0,
        double verticalSpeedFpm = 0) => new(
        Timestamp: DateTimeOffset.UtcNow,
        OnGround: onGround,
        IasKt: gsKt,
        GsKt: gsKt,
        VerticalSpeedFpm: verticalSpeedFpm,
        GLoad: 1.0,
        OatC: 15,
        EngineRpm: engineRpm,
        EngineN1Pct: n1Pct,
        IttC: -1,
        TorqueFtLb: -1,
        FuelFlowPph: 50,
        OilTempC: oilTempC,
        OilPressurePsi: 60,
        GroundspeedAtTouchdownKt: groundspeedAtTouchdownKt,
        BrakeEnergyJoules: brakeEnergyJoules);

    // -------------------------------------------------------------------------
    // Test 1 — Hobbs accumulates at correct rate
    // -------------------------------------------------------------------------

    [Fact]
    public void WearEngine_Hobbs_AccumulatesAtCorrectRate()
    {
        // Arrange: 3600 ticks at 1 second each, engine running.
        var engine = new WearEngine();
        var airframe = MakeAirframe();
        var components = new[] { MakeComponent(airframe.Id) };
        var consumables = Array.Empty<Consumable>();
        var accelerators = Array.Empty<Accelerator>();
        var sample = MakeEngineSample(engineRpm: 2300);
        var dt = TimeSpan.FromSeconds(1);

        double totalHobbs = 0.0;
        for (int i = 0; i < 3600; i++)
        {
            var result = engine.Tick(airframe, components, consumables, accelerators, sample, dt);
            totalHobbs += result.HobbsHoursDelta;
        }

        // 3600 seconds = 1.0 hour exactly.
        Assert.InRange(totalHobbs, 1.0 - 0.001, 1.0 + 0.001);
    }

    // -------------------------------------------------------------------------
    // Test 2 — Oil overtemp doubles burn rate
    // -------------------------------------------------------------------------

    [Fact]
    public void WearEngine_Oil_OvertempDoublesBurnRate()
    {
        // Arrange: two consumables (same capacity), one at 95 °C, one at 130 °C oil temp.
        // At 95 °C: overtempFactor = 1.0.
        // At 130 °C: overtempFactor = 1 + (130-100)/30 = 2.0 → double burn rate.

        var coolFactor = WearEngine.ComputeOvertempFactor(95.0);   // 1.0
        var hotFactor  = WearEngine.ComputeOvertempFactor(130.0);  // 2.0

        Assert.Equal(1.0, coolFactor, precision: 6);
        Assert.Equal(2.0, hotFactor,  precision: 6);

        // Now verify the full engine path produces ~2× the consumption at 130 °C vs 95 °C.
        var engine = new WearEngine();
        var airframe = MakeAirframe();
        var components = new[] { MakeComponent(airframe.Id) };
        var accelerators = Array.Empty<Accelerator>();
        var dt = TimeSpan.FromHours(1);

        var oilConsumable = MakeConsumable(airframe.Id, ConsumableKind.Oil, level: 1.0, capacity: 8.0);
        var consumables = new[] { oilConsumable };

        // Cool run at 95 °C
        var coolSample = MakeEngineSample(n1Pct: 90, oilTempC: 95);
        var coolResult = engine.Tick(airframe, components, consumables, accelerators, coolSample, dt);
        double coolDelta = coolResult.ConsumableLevelDeltas.TryGetValue(oilConsumable.Id, out var cv) ? Math.Abs(cv) : 0;

        // Hot run at 130 °C
        var hotSample = MakeEngineSample(n1Pct: 90, oilTempC: 130);
        var hotResult = engine.Tick(airframe, components, consumables, accelerators, hotSample, dt);
        double hotDelta = hotResult.ConsumableLevelDeltas.TryGetValue(oilConsumable.Id, out var hv) ? Math.Abs(hv) : 0;

        // Hot burn should be ~2× cool burn (within 1% rounding).
        Assert.True(coolDelta > 0, "Cool oil delta should be positive");
        Assert.True(hotDelta > 0, "Hot oil delta should be positive");

        double ratio = hotDelta / coolDelta;
        Assert.InRange(ratio, 1.98, 2.02);
    }

    // -------------------------------------------------------------------------
    // Test 3 — Tire touchdown wear scales with GS^2
    // -------------------------------------------------------------------------

    [Fact]
    public void WearEngine_Tires_TouchdownWearScalesWithSquare()
    {
        // 60 kt vs 120 kt — should give 4× wear (quadratic formula).

        var sample60 = MakeEngineSample(groundspeedAtTouchdownKt: 60);
        var sample120 = MakeEngineSample(groundspeedAtTouchdownKt: 120);
        var dt = TimeSpan.FromMilliseconds(250); // 4 Hz tick, no taxi contribution

        double wear60  = WearEngine.ComputeTireWearDelta(sample60,  dt.TotalSeconds);
        double wear120 = WearEngine.ComputeTireWearDelta(sample120, dt.TotalSeconds);

        Assert.True(wear60  > 0, "60 kt wear should be positive");
        Assert.True(wear120 > 0, "120 kt wear should be positive");

        // Expected ratio: (120/60)^2 = 4.0 — from touchdown spike only.
        // Taxi contribution at 250 ms is negligible (GsKt = 0 in sample).
        double ratio = wear120 / wear60;
        Assert.InRange(ratio, 3.98, 4.02);
    }

    // -------------------------------------------------------------------------
    // Test 4 — Brake wear accumulates linearly with energy
    // -------------------------------------------------------------------------

    [Fact]
    public void WearEngine_Brakes_AccumulatesJoulesLinearly()
    {
        // BrakeEnergyJoules is applied as a simple linear fraction.
        // 1 MJ → BrakeWearPerMegajoule wear increment.
        // 2 MJ → 2 × BrakeWearPerMegajoule.

        var sample1MJ = MakeEngineSample(brakeEnergyJoules: 1_000_000);
        var sample2MJ = MakeEngineSample(brakeEnergyJoules: 2_000_000);

        double delta1 = WearEngine.ComputeBrakeWearDelta(sample1MJ);
        double delta2 = WearEngine.ComputeBrakeWearDelta(sample2MJ);

        Assert.True(delta1 > 0, "1 MJ brake delta should be positive");
        Assert.Equal(2.0, delta2 / delta1, precision: 9);

        // Sanity: zero energy → zero wear.
        var sampleZero = MakeEngineSample(brakeEnergyJoules: 0);
        Assert.Equal(0.0, WearEngine.ComputeBrakeWearDelta(sampleZero));
    }

    // -------------------------------------------------------------------------
    // Test 5 — Stochastic accumulator is deterministic with fixed accelerators
    // -------------------------------------------------------------------------

    [Fact]
    public void WearEngine_Stochastic_DeterministicWithFixedAccelerator()
    {
        // Identical inputs must produce identical outputs on every call.
        // WearEngine contains no RNG; determinism is guaranteed by its pure-function design.

        var engine = new WearEngine();
        var airframe = MakeAirframe();
        var components = new[] { MakeComponent(airframe.Id) };
        var consumables = Array.Empty<Consumable>();

        // Accelerator: ITT > 712 °C → multiplier 1.5 (typical turbine hot-section guard).
        var accelerator = new Accelerator
        {
            Id = Guid.NewGuid(),
            Category = ComponentCategory.Engine,
            Variable = "IttC",
            Formula = """[{"trigger":"IttC","threshold":712,"multiplier":1.5}]"""
        };
        var accelerators = new[] { accelerator };

        var sample = MakeEngineSample(n1Pct: 85) with { IttC = 720 }; // above 712 → active
        var dt = TimeSpan.FromMilliseconds(250);

        // Run twice with identical inputs.
        var result1 = engine.Tick(airframe, components, consumables, accelerators, sample, dt);
        var result2 = engine.Tick(airframe, components, consumables, accelerators, sample, dt);

        // Same component wear deltas.
        Assert.Equal(result1.ComponentWearDeltas.Count, result2.ComponentWearDeltas.Count);
        foreach (var (id, delta1) in result1.ComponentWearDeltas)
        {
            Assert.True(result2.ComponentWearDeltas.TryGetValue(id, out double delta2));
            Assert.Equal(delta1, delta2);
        }

        // Same Hobbs.
        Assert.Equal(result1.HobbsHoursDelta, result2.HobbsHoursDelta);

        // Verify accelerator multiplier is 1.5 when IttC = 720 > 712.
        double multiplier = WearEngine.EvaluateAccelerators(accelerators, sample);
        Assert.Equal(1.5, multiplier, precision: 9);

        // And 1.0 (no acceleration) when IttC = 700 < 712.
        var coolSample = sample with { IttC = 700 };
        double neutralMultiplier = WearEngine.EvaluateAccelerators(accelerators, coolSample);
        Assert.Equal(1.0, neutralMultiplier, precision: 9);
    }
}

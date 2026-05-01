using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MsfsFailures.Data.Entities;

namespace MsfsFailures.Data.Seeding;

/// <summary>
/// Idempotent seed: runs once on first launch to populate the five demo airframes
/// and seven squawks sourced from MockFleetSource. If the Airframes table is
/// non-empty the method returns immediately.
/// </summary>
public static class SeedIfEmpty
{
    // Consumable.Kind int mapping (mirrors future Core.ConsumableKind enum):
    //  0 = Oil, 1 = Tires, 2 = Brakes, 3 = Battery, 4 = Hydraulic

    public static async Task ApplyAsync(
        FleetDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await db.Airframes.AnyAsync(ct))
        {
            logger.LogInformation("Fleet already seeded — skipping.");
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // ── ModelRefs ────────────────────────────────────────────────────
        // Widened to catch all Asobo C172 variants: "Asobo Cessna 172 G1000", "Cessna_172",
        // "CESSNA 172 SKYHAWK", "C172" ATC MODEL codes, etc.
        var refC172  = new ModelRef { Id = Guid.NewGuid(), Name = "Cessna 172S Skyhawk",         Manufacturer = "Cessna",   SimMatchRulesJson = """{"contains":["172","Skyhawk","Cessna_172","C172","CESSNA 172"]}""" };
        var refPa28  = new ModelRef { Id = Guid.NewGuid(), Name = "Piper PA-28-181 Archer III",  Manufacturer = "Piper",    SimMatchRulesJson = """{"contains":["PA28","Archer"]}""" };
        var refBe350 = new ModelRef { Id = Guid.NewGuid(), Name = "Beechcraft King Air 350",     Manufacturer = "Beechcraft", SimMatchRulesJson = """{"contains":["350","King Air"]}""" };
        var refBe58  = new ModelRef { Id = Guid.NewGuid(), Name = "Black Square Baron 58",       Manufacturer = "Beechcraft", SimMatchRulesJson = """{"contains":["58","Baron"]}""" };
        var refC210  = new ModelRef { Id = Guid.NewGuid(), Name = "Black Square Cessna 210",     Manufacturer = "Cessna",   SimMatchRulesJson = """{"contains":["210","Centurion"]}""" };

        db.ModelRefs.AddRange(refC172, refPa28, refBe350, refBe58, refC210);

        // ── Airframes ────────────────────────────────────────────────────
        var afN172AB = new Airframe { Id = Guid.NewGuid(), Tail = "N172AB", Type = "C172",   ModelRef = refC172,  TotalHobbsHours = 1842.6, TotalCycles = 2104, CreatedAt = now };
        var afN812RP = new Airframe { Id = Guid.NewGuid(), Tail = "N812RP", Type = "PA-28",  ModelRef = refPa28,  TotalHobbsHours = 3104.2, TotalCycles = 4188, CreatedAt = now };
        var afN350KA = new Airframe { Id = Guid.NewGuid(), Tail = "N350KA", Type = "BE350",  ModelRef = refBe350, TotalHobbsHours = 5621.8, TotalCycles = 3812, CreatedAt = now };
        var afN58BS  = new Airframe { Id = Guid.NewGuid(), Tail = "N58BS",  Type = "BE58",   ModelRef = refBe58,  TotalHobbsHours = 2487.5, TotalCycles = 2998, CreatedAt = now };
        var afN210BS = new Airframe { Id = Guid.NewGuid(), Tail = "N210BS", Type = "C210",   ModelRef = refC210,  TotalHobbsHours = 4218.0, TotalCycles = 3604, CreatedAt = now };

        db.Airframes.AddRange(afN172AB, afN812RP, afN350KA, afN58BS, afN210BS);
        logger.LogInformation("Seeding 5 airframes…");

        // ── Consumables (Oil=0, Tires=1, Brakes=2, Battery=3, Hydraulic=4) ──
        // MockFleetSource order: ConsumablesVm(Oil, Tires, Brakes, Battery, Hydraulic)
        AddConsumables(db, afN172AB, now, oil: 0.78, tires: 0.62, brakes: 0.55, battery: 0.91, hyd: 0.99);
        AddConsumables(db, afN812RP, now, oil: 0.41, tires: 0.34, brakes: 0.22, battery: 0.74, hyd: 0.88);
        AddConsumables(db, afN350KA, now, oil: 0.66, tires: 0.71, brakes: 0.68, battery: 0.88, hyd: 0.94);
        AddConsumables(db, afN58BS,  now, oil: 0.31, tires: 0.48, brakes: 0.39, battery: 0.62, hyd: 0.71);
        AddConsumables(db, afN210BS, now, oil: 0.52, tires: 0.18, brakes: 0.12, battery: 0.44, hyd: 0.88);

        // ── Squawks (Status: 0=Open, 1=Deferred, 2=Grounding) ───────────
        // SQ-1042: N812RP — Open
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN812RP, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 30, 0, 0, 0, TimeSpan.Zero),
            Status = 0,
            HoursAtOpen = 3102.1,
            Notes = "Rough on runup, ~75 RPM drop on right mag, intermittent miss. " +
                    "Component: #2 Magneto. Reproduced on last two starts. Suspect plug fouling or mag timing drift. MEL-deferrable: true",
        });

        // SQ-1043: N812RP — Deferred
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN812RP, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero),
            DeferredUntil = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            Status = 1,
            HoursAtOpen = 3088.9,
            Notes = "Soft pedal feel, slight pull right on heavy braking. " +
                    "Component: Right Brake. MEL deferral. Pads at 22% — schedule replacement next inspection. MEL-deferrable: true",
        });

        // SQ-1044: N350KA — Deferred
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN350KA, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 27, 0, 0, 0, TimeSpan.Zero),
            DeferredUntil = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero),
            Status = 1,
            HoursAtOpen = 5613.4,
            Notes = "Slow climb to scheduled differential, +12 sec vs nominal. " +
                    "Component: Cabin Pressurization. Within MEL limits. Outflow valve service due at next phase inspection. MEL-deferrable: true",
        });

        // SQ-1045: N58BS — Open
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN58BS, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 28, 0, 0, 0, TimeSpan.Zero),
            Status = 0,
            HoursAtOpen = 2486.0,
            Notes = "CHT exceeded 420°F twice during climb out of KSEZ. " +
                    "Component: L Engine — Cyl #4 CHT. Recommend cowl flap inspection + climb profile review. Two overtemps logged. MEL-deferrable: false",
        });

        // SQ-1046: N210BS — Grounding
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN210BS, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero),
            Status = 2,
            HoursAtOpen = 4217.4,
            Notes = "Tread at 18% — below replacement threshold. " +
                    "Component: Main Tires (both). Hard landing logged on prior session (-621 fpm). Inspect strut packings. MEL-deferrable: false",
        });

        // SQ-1047: N210BS — Grounding
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN210BS, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero),
            Status = 2,
            HoursAtOpen = 4217.4,
            Notes = "State-of-health 44% — slow cranking, two deep-discharge events. " +
                    "Component: Battery SOH. Replace before next flight. Concorde RG-35AXC on order. MEL-deferrable: false",
        });

        // SQ-1048: N210BS — Grounding
        db.Squawks.Add(new Squawk
        {
            Id = Guid.NewGuid(), Airframe = afN210BS, FailureModeId = null,
            Opened = new DateTimeOffset(2026, 4, 22, 0, 0, 0, TimeSpan.Zero),
            Status = 2,
            HoursAtOpen = 4217.4,
            Notes = "Suction reading 4.2 inHg, below 4.5 minimum. " +
                    "Component: Vacuum Pump. Pump at 612 hours TBO. Replacement scheduled. MEL-deferrable: false",
        });

        logger.LogInformation("Seeding 7 squawks…");

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seed complete — 5 airframes, 7 squawks written to database.");
    }

    private static void AddConsumables(
        FleetDbContext db,
        Airframe airframe,
        DateTimeOffset now,
        double oil,
        double tires,
        double brakes,
        double battery,
        double hyd)
    {
        db.Consumables.AddRange(
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 0, Level = oil,    Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 1, Level = tires,  Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 2, Level = brakes, Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 3, Level = battery,Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 4, Level = hyd,    Capacity = 1.0, LastTopUpAt = now }
        );
    }
}

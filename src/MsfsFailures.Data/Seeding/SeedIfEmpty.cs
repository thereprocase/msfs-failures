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

    // ComponentCategory int mapping (Core.ComponentCategory enum):
    //  0=Engine, 1=HotSection, 2=Compressor, 3=OilSystem, 4=FuelSystem,
    //  5=Propeller, 6=GearBrakes, 7=Tires, 8=Battery, 9=Hydraulic, 10=Avionics, 11=Other
    private static class Cat
    {
        public const int Engine     = 0;
        public const int HotSection = 1;
        public const int Compressor = 2;
        public const int OilSystem  = 3;
        public const int FuelSystem = 4;
        public const int Propeller  = 5;
        public const int GearBrakes = 6;
        public const int Tires      = 7;
        public const int Battery    = 8;
        public const int Hydraulic  = 9;
        public const int Avionics   = 10;
        public const int Other      = 11;
    }

    public static async Task ApplyAsync(
        FleetDbContext db,
        ILogger logger,
        CancellationToken ct = default)
    {
        // Backfill SimMatchRulesJson on existing ModelRef rows (idempotent — only updates
        // rows where the field is null/empty, so safe to run on every startup).
        await BackfillMatchRulesAsync(db, logger, ct);

        // Backfill ComponentTemplates + Components (idempotent — skips if already seeded).
        // Called here for the existing-DB case (airframes present, no templates yet).
        await BackfillComponentsAsync(db, logger, ct);

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

        // Fresh-seed path: airframes were just written; now seed templates + components.
        // BackfillComponentsAsync will find the newly-saved rows.
        await BackfillComponentsAsync(db, logger, ct);

        logger.LogInformation("Seed complete — 5 airframes, 7 squawks, templates and components written to database.");
    }

    // ── SimMatchRulesJson backfill ────────────────────────────────────────────

    /// <summary>
    /// Backfills <see cref="ModelRef.SimMatchRulesJson"/> on rows that were created before
    /// match rules were added to the seed.  Keyed by Name substrings that uniquely identify
    /// each seeded ModelRef.  Safe to run every startup — only touches rows where the field
    /// is currently null/empty.
    /// </summary>
    private static async Task BackfillMatchRulesAsync(FleetDbContext db, ILogger logger, CancellationToken ct)
    {
        // Maps a name-substring that uniquely identifies the ModelRef → canonical JSON.
        // The substring check is intentionally loose so it survives minor name edits.
        var canonical = new (string Substring, string Json)[]
        {
            ("172",      """{"contains":["172","Skyhawk","Cessna_172","C172","CESSNA 172"]}"""),
            ("PA-28",    """{"contains":["PA28","Archer","PA-28"]}"""),
            ("King Air", """{"contains":["350","King Air","KingAir"]}"""),
            ("Baron",    """{"contains":["58","Baron"]}"""),
            // "Cessna 210" or "Centurion" — match on "210" or "Centurion" in name
            ("210",      """{"contains":["210","Centurion"]}"""),
        };

        var refs = await db.ModelRefs.ToListAsync(ct);
        var updated = 0;
        foreach (var r in refs)
        {
            // Skip rows that already have meaningful match rules.
            // Treat null, empty, whitespace, and the bare "{}" stub as "not yet set".
            if (!string.IsNullOrWhiteSpace(r.SimMatchRulesJson) &&
                r.SimMatchRulesJson.Trim() != "{}")
                continue;

            foreach (var (sub, json) in canonical)
            {
                if (r.Name.Contains(sub, StringComparison.OrdinalIgnoreCase) ||
                    r.Manufacturer.Contains(sub, StringComparison.OrdinalIgnoreCase))
                {
                    r.SimMatchRulesJson = json;
                    updated++;
                    break;
                }
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("SeedIfEmpty: backfilled SimMatchRulesJson on {Count} ModelRef row(s).", updated);
        }
    }

    // ── ComponentTemplate + Component backfill ────────────────────────────────

    /// <summary>
    /// Seeds ComponentTemplate rows for each ModelRef and Component rows for each Airframe.
    /// Idempotent: skips entirely if ComponentTemplates table is non-empty.
    /// MTBF values and Weibull parameters are sourced from NASA CR-2001-210647 via
    /// WeibullDefaults in MsfsFailures.Core.
    /// </summary>
    private static async Task BackfillComponentsAsync(
        FleetDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        if (await db.ComponentTemplates.AnyAsync(ct)) return;

        var modelRefs = await db.ModelRefs.ToListAsync(ct);
        var airframes = await db.Airframes.ToListAsync(ct);

        if (modelRefs.Count == 0 || airframes.Count == 0) return;

        // Locate each model ref by name substring.
        ModelRef? FindRef(string substring) =>
            modelRefs.FirstOrDefault(r => r.Name.Contains(substring, StringComparison.OrdinalIgnoreCase));

        var refC172  = FindRef("172");
        var refPa28  = FindRef("PA-28");
        var refBe350 = FindRef("King Air");
        var refBe58  = FindRef("Baron");
        var refC210  = FindRef("Cessna 210");
        // Fallback: Baron and 210 share "Cessna" — use "210" directly if above fails.
        if (refC210 == null)
            refC210 = FindRef("210");

        // WearCurveJson helper — encodes Weibull parameters as JSON blob.
        static string WearJson(double beta, double alpha, double? oilQtPerHour = null)
        {
            var oil = oilQtPerHour.HasValue
                ? $",\"oilQtPerHourBase\":{oilQtPerHour.Value}"
                : string.Empty;
            return $"{{\"weibullBeta\":{beta},\"weibullAlphaHours\":{alpha}{oil}}}";
        }

        // Template factory.
        static ComponentTemplate T(
            ModelRef mr,
            string name,
            int cat,
            double mtbf,
            double beta,
            double alpha,
            double? oilQtPerHour = null,
            double? replaceHours = null,
            int? replaceCycles = null) =>
            new ComponentTemplate
            {
                Id                    = Guid.NewGuid(),
                ModelRefId            = mr.Id,
                Name                  = name,
                Category              = cat,
                MtbfHours             = mtbf,
                WearCurveJson         = WearJson(beta, alpha, oilQtPerHour),
                ConsumableKind        = 0,
                ReplaceIntervalHours  = replaceHours,
                ReplaceIntervalCycles = replaceCycles,
            };

        var templates = new List<ComponentTemplate>();

        // ── C172 (Lycoming O-320, fixed gear) ──────────────────────────────
        // Piston: no compressor section. Hot section = cylinder/exhaust assembly.
        if (refC172 != null)
        {
            templates.AddRange(new[]
            {
                T(refC172, "Engine",        Cat.Engine,     2000, 1.58, 2000, oilQtPerHour: 0.08, replaceHours: 2000),
                T(refC172, "Hot Section",   Cat.HotSection, 3500, 1.80, 3500),
                T(refC172, "Oil System",    Cat.OilSystem,  4000, 1.14, 3977),
                T(refC172, "Fuel System",   Cat.FuelSystem, 5130, 1.44, 5130, oilQtPerHour: 0.08),
                T(refC172, "Propeller",     Cat.Propeller,  2400, 1.63, 2400, replaceHours: 2400),
                T(refC172, "Gear & Brakes", Cat.GearBrakes, 1500, 0.92, 1500),
                T(refC172, "Battery",       Cat.Battery,    1500, 0.90, 4000),
                T(refC172, "Avionics",      Cat.Avionics,   4950, 1.67, 4950),
            });
        }

        // ── PA-28 (Lycoming O-360, fixed gear) — same shape as C172 ────────
        if (refPa28 != null)
        {
            templates.AddRange(new[]
            {
                T(refPa28, "Engine",        Cat.Engine,     2000, 1.58, 2000, oilQtPerHour: 0.08, replaceHours: 2000),
                T(refPa28, "Hot Section",   Cat.HotSection, 3500, 1.80, 3500),
                T(refPa28, "Oil System",    Cat.OilSystem,  4000, 1.14, 3977),
                T(refPa28, "Fuel System",   Cat.FuelSystem, 5130, 1.44, 5130, oilQtPerHour: 0.08),
                T(refPa28, "Propeller",     Cat.Propeller,  2400, 1.63, 2400, replaceHours: 2400),
                T(refPa28, "Gear & Brakes", Cat.GearBrakes, 1500, 0.92, 1500),
                T(refPa28, "Battery",       Cat.Battery,    1500, 0.90, 4000),
                T(refPa28, "Avionics",      Cat.Avionics,   4950, 1.67, 4950),
            });
        }

        // ── BE350 (PT6A turboprop, retractable gear) ────────────────────────
        // Turbine: full hot-section + compressor sections; hydraulic system.
        if (refBe350 != null)
        {
            templates.AddRange(new[]
            {
                T(refBe350, "Engine",        Cat.Engine,     3500, 1.58, 3500, oilQtPerHour: 0.10, replaceHours: 3500),
                T(refBe350, "Hot Section",   Cat.HotSection, 1800, 1.80, 1800, replaceHours: 1800),
                T(refBe350, "Compressor",    Cat.Compressor, 2200, 1.60, 2200, oilQtPerHour: 0.10),
                T(refBe350, "Oil System",    Cat.OilSystem,  3000, 1.14, 3977),
                T(refBe350, "Fuel System",   Cat.FuelSystem, 5000, 1.44, 5130),
                T(refBe350, "Propeller",     Cat.Propeller,  2400, 1.63, 2400, replaceHours: 2400),
                T(refBe350, "Gear & Brakes", Cat.GearBrakes, 1500, 0.92, 1500),
                T(refBe350, "Battery",       Cat.Battery,    1500, 0.90, 4000),
                T(refBe350, "Avionics",      Cat.Avionics,   4950, 1.67, 4950),
                T(refBe350, "Hydraulic",     Cat.Hydraulic,  3977, 1.14, 3977),
            });
        }

        // ── BE58 Baron (twin piston) ────────────────────────────────────────
        // Two engines + two props modeled separately. MTBFs slightly more aggressive.
        if (refBe58 != null)
        {
            templates.AddRange(new[]
            {
                T(refBe58, "L Engine",      Cat.Engine,     1800, 1.58, 1800, oilQtPerHour: 0.09, replaceHours: 1800),
                T(refBe58, "R Engine",      Cat.Engine,     1800, 1.58, 1800, oilQtPerHour: 0.09, replaceHours: 1800),
                T(refBe58, "Hot Section",   Cat.HotSection, 3200, 1.80, 3200),
                T(refBe58, "Oil System",    Cat.OilSystem,  3600, 1.14, 3977),
                T(refBe58, "Fuel System",   Cat.FuelSystem, 4800, 1.44, 5130, oilQtPerHour: 0.08),
                T(refBe58, "L Propeller",   Cat.Propeller,  2400, 1.63, 2400, replaceHours: 2400),
                T(refBe58, "R Propeller",   Cat.Propeller,  2400, 1.63, 2400, replaceHours: 2400),
                T(refBe58, "Gear & Brakes", Cat.GearBrakes, 1500, 0.92, 1500),
                T(refBe58, "Battery",       Cat.Battery,    1500, 0.90, 4000),
                T(refBe58, "Avionics",      Cat.Avionics,   4950, 1.67, 4950),
            });
        }

        // ── C210 (Continental IO-520, retractable gear) ─────────────────────
        // Similar to C172 but IO-520 TBO is 1700 hr; hydraulic retract gear.
        if (refC210 != null)
        {
            templates.AddRange(new[]
            {
                T(refC210, "Engine",        Cat.Engine,     1700, 1.58, 1700, oilQtPerHour: 0.08, replaceHours: 1700),
                T(refC210, "Hot Section",   Cat.HotSection, 3200, 1.80, 3200),
                T(refC210, "Oil System",    Cat.OilSystem,  3800, 1.14, 3977),
                T(refC210, "Fuel System",   Cat.FuelSystem, 5130, 1.44, 5130, oilQtPerHour: 0.08),
                T(refC210, "Propeller",     Cat.Propeller,  2400, 1.63, 2400, replaceHours: 2400),
                T(refC210, "Gear & Brakes", Cat.GearBrakes, 1500, 0.92, 1500),
                T(refC210, "Battery",       Cat.Battery,    1500, 0.90, 4000),
                T(refC210, "Avionics",      Cat.Avionics,   4950, 1.67, 4950),
                T(refC210, "Hydraulic",     Cat.Hydraulic,  3977, 1.14, 3977),
            });
        }

        if (templates.Count == 0) return;

        db.ComponentTemplates.AddRange(templates);

        // ── Component rows per airframe ─────────────────────────────────────
        // Group templates by the ModelRef they belong to.
        var templatesByRef = templates
            .GroupBy(t => t.ModelRefId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var components = new List<Component>();

        foreach (var af in airframes)
        {
            if (!templatesByRef.TryGetValue(af.ModelRefId, out var afTemplates))
                continue;

            var isN172AB = af.Tail.Equals("N172AB", StringComparison.OrdinalIgnoreCase);

            foreach (var tmpl in afTemplates)
            {
                double wear;
                if (isN172AB)
                {
                    // Nuanced wear for the primary demo airframe to make the dashboard
                    // look interesting on first launch.
                    wear = tmpl.Category switch
                    {
                        Cat.Engine     => 0.35,   // approaching mid-life
                        Cat.Battery    => 0.55,   // getting old
                        Cat.Tires      => 0.62,   // matches consumable
                        Cat.GearBrakes => 0.55,   // matches consumable
                        _              => 0.20,
                    };
                }
                else
                {
                    // Generic heuristic: wear = clamp(Hobbs / (MTBF * 2), 0, 0.85)
                    wear = Math.Clamp(af.TotalHobbsHours / (tmpl.MtbfHours * 2.0), 0.0, 0.85);
                }

                components.Add(new Component
                {
                    Id             = Guid.NewGuid(),
                    AirframeId     = af.Id,
                    TemplateId     = tmpl.Id,
                    Hours          = af.TotalHobbsHours,
                    Cycles         = af.TotalCycles,
                    Wear           = wear,
                    Condition      = "nominal",
                    LastServicedAt = af.CreatedAt,
                    InstalledAt    = af.CreatedAt,
                });
            }
        }

        db.Components.AddRange(components);
        await db.SaveChangesAsync(ct);

        var refCount = templates.Select(t => t.ModelRefId).Distinct().Count();
        logger.LogInformation(
            "BackfillComponents: seeded {TemplateCount} templates across {RefCount} model refs.",
            templates.Count, refCount);
        logger.LogInformation(
            "BackfillComponents: seeded {ComponentCount} components across {AirframeCount} airframes.",
            components.Count, airframes.Count);
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
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 0, Level = oil,     Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 1, Level = tires,   Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 2, Level = brakes,  Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 3, Level = battery, Capacity = 1.0, LastTopUpAt = now },
            new Consumable { Id = Guid.NewGuid(), Airframe = airframe, Kind = 4, Level = hyd,     Capacity = 1.0, LastTopUpAt = now }
        );
    }
}

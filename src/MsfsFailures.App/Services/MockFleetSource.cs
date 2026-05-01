using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App.Services;

// Verbatim port of design/2026-05-01-home-screen/project/data.jsx.
public sealed class MockFleetSource : IFleetSource
{
    public IReadOnlyList<AirframeVm> GetAirframes() => Airframes;
    public IReadOnlyList<SquawkVm> GetSquawks() => Squawks;

    private static readonly AirframeVm[] Airframes =
    [
        new AirframeVm
        {
            Id = "N172AB", Tail = "N172AB", Type = "C172",
            Model = "Cessna 172S Skyhawk", Nickname = "Old Faithful",
            Status = AirframeStatus.Airworthy,
            Hours = 1842.6, Cycles = 2104,
            HobbsSinceMx = 24.3, NextInspectionHrs = 12.7,
            OpenSquawks = 0, Deferred = 0,
            Consumables = new ConsumablesVm(0.78, 0.62, 0.55, 0.91, 0.99),
            LastFlight = new LastFlightVm("2026-04-29", "1:42", 1.4, 0, 0),
            Live = false,
        },
        new AirframeVm
        {
            Id = "N812RP", Tail = "N812RP", Type = "PA-28",
            Model = "Piper PA-28-181 Archer III", Nickname = "Romeo Papa",
            Status = AirframeStatus.Squawks,
            Hours = 3104.2, Cycles = 4188,
            HobbsSinceMx = 47.1, NextInspectionHrs = 2.9,
            OpenSquawks = 2, Deferred = 1,
            Consumables = new ConsumablesVm(0.41, 0.34, 0.22, 0.74, 0.88),
            LastFlight = new LastFlightVm("2026-04-30", "0:58", 1.9, 1, 1),
            Live = false,
        },
        new AirframeVm
        {
            Id = "N350KA", Tail = "N350KA", Type = "BE350",
            Model = "Beechcraft King Air 350", Nickname = "The Beech",
            Status = AirframeStatus.Airworthy,
            Hours = 5621.8, Cycles = 3812,
            HobbsSinceMx = 8.4, NextInspectionHrs = 41.6,
            OpenSquawks = 1, Deferred = 1,
            Consumables = new ConsumablesVm(0.66, 0.71, 0.68, 0.88, 0.94),
            LastFlight = new LastFlightVm("2026-04-30", "2:14", 1.6, 0, 0),
            Live = true,
            LiveState = new LiveStateVm(
                Phase: "CRUISE",
                Altitude: 24000,
                Ias: 218,
                Gs: 252,
                Hdg: 287,
                Oat: -28,
                FuelKg: 1042,
                N1: [88.4, 88.1],
                Itt: [712, 706],
                Torque: [82, 81],
                OilTemp: [78, 76],
                OilPress: [82, 84],
                Gear: "UP",
                Flaps: 0,
                HobbsStart: 5619.6,
                CurrentHobbs: 5621.8),
        },
        new AirframeVm
        {
            Id = "N58BS", Tail = "N58BS", Type = "BE58",
            Model = "Black Square Baron 58", Nickname = "Twin Sister",
            Status = AirframeStatus.MxDue,
            Hours = 2487.5, Cycles = 2998,
            HobbsSinceMx = 99.2, NextInspectionHrs = 0.8,
            OpenSquawks = 1, Deferred = 0,
            Consumables = new ConsumablesVm(0.31, 0.48, 0.39, 0.62, 0.71),
            LastFlight = new LastFlightVm("2026-04-28", "1:18", 2.1, 0, 2),
            Live = false,
        },
        new AirframeVm
        {
            Id = "N210BS", Tail = "N210BS", Type = "C210",
            Model = "Black Square Cessna 210", Nickname = "The Centurion",
            Status = AirframeStatus.Grounded,
            Hours = 4218.0, Cycles = 3604,
            HobbsSinceMx = 4.2, NextInspectionHrs = 45.8,
            OpenSquawks = 3, Deferred = 0,
            Consumables = new ConsumablesVm(0.52, 0.18, 0.12, 0.44, 0.88),
            LastFlight = new LastFlightVm("2026-04-22", "0:34", 2.4, 1, 0),
            Live = false,
        },
    ];

    private static readonly SquawkVm[] Squawks =
    [
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1042", Tail = "N812RP",
            Component = "#2 Magneto",
            Summary = "Rough on runup, ~75 RPM drop on right mag, intermittent miss.",
            Severity = SquawkSeverity.Open, MelDeferrable = true,
            Opened = "2026-04-30", HoursAtOpen = 3102.1,
            Notes = "Reproduced on last two starts. Suspect plug fouling or mag timing drift.",
        },
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1043", Tail = "N812RP",
            Component = "Right Brake",
            Summary = "Soft pedal feel, slight pull right on heavy braking.",
            Severity = SquawkSeverity.Deferred, MelDeferrable = true,
            Opened = "2026-04-26", HoursAtOpen = 3088.9, DeferredUntil = "2026-05-10",
            Notes = "MEL deferral. Pads at 22% — schedule replacement next inspection.",
        },
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1044", Tail = "N350KA",
            Component = "Cabin Pressurization",
            Summary = "Slow climb to scheduled differential, +12 sec vs nominal.",
            Severity = SquawkSeverity.Deferred, MelDeferrable = true,
            Opened = "2026-04-27", HoursAtOpen = 5613.4, DeferredUntil = "2026-05-15",
            Notes = "Within MEL limits. Outflow valve service due at next phase inspection.",
        },
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1045", Tail = "N58BS",
            Component = "L Engine — Cyl #4 CHT",
            Summary = "CHT exceeded 420°F twice during climb out of KSEZ.",
            Severity = SquawkSeverity.Open, MelDeferrable = false,
            Opened = "2026-04-28", HoursAtOpen = 2486.0,
            Notes = "Recommend cowl flap inspection + climb profile review. Two overtemps logged.",
        },
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1046", Tail = "N210BS",
            Component = "Main Tires (both)",
            Summary = "Tread at 18% — below replacement threshold.",
            Severity = SquawkSeverity.Grounding, MelDeferrable = false,
            Opened = "2026-04-22", HoursAtOpen = 4217.4,
            Notes = "Hard landing logged on prior session (-621 fpm). Inspect strut packings.",
        },
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1047", Tail = "N210BS",
            Component = "Battery SOH",
            Summary = "State-of-health 44% — slow cranking, two deep-discharge events.",
            Severity = SquawkSeverity.Grounding, MelDeferrable = false,
            Opened = "2026-04-22", HoursAtOpen = 4217.4,
            Notes = "Replace before next flight. Concorde RG-35AXC on order.",
        },
        new SquawkVm
        {
            SquawkGuid = Guid.NewGuid(), Id = "SQ-1048", Tail = "N210BS",
            Component = "Vacuum Pump",
            Summary = "Suction reading 4.2 inHg, below 4.5 minimum.",
            Severity = SquawkSeverity.Grounding, MelDeferrable = false,
            Opened = "2026-04-22", HoursAtOpen = 4217.4,
            Notes = "Pump at 612 hours TBO. Replacement scheduled.",
        },
    ];
}

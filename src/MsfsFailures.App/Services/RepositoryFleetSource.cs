// TODO (future passes — synthetic / cosmetic fields not yet populated from DB):
//   - AirframeVm.Nickname       → populate from a future airframes.nickname column or separate metadata table
//   - AirframeVm.Live / LiveState→ once SessionService exists, set Live = airframe has open session.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.Core;
using MsfsFailures.Data;
using Entities = MsfsFailures.Data.Entities;
using MsfsFailures.Data.Repositories;
using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App.Services;

/// <summary>
/// <see cref="IFleetSource"/> backed by <see cref="IFleetRepository"/> (SQLite).
/// Reads on construction (sync-over-async) which is acceptable at app startup
/// after the host has started and migrations/seed have run.
/// TODO: live refresh on a timer or via DB-change hook.
/// </summary>
public sealed class RepositoryFleetSource : IFleetSource
{
    private readonly IServiceScopeFactory _scopeFactory;
    private IReadOnlyList<AirframeVm> _airframes;
    private IReadOnlyList<SquawkVm> _squawks;

    public RepositoryFleetSource(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        // Initial load on construction (sync-over-async pattern).
        // In real apps with async DI, this should be deferred to an async initializer.
        _airframes = LoadAirframesSync();
        _squawks   = LoadSquawksSync();
    }

    public IReadOnlyList<AirframeVm> GetAirframes() => _airframes;
    public IReadOnlyList<SquawkVm>   GetSquawks()   => _squawks;

    private IReadOnlyList<AirframeVm> LoadAirframesSync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
        var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();

        // Load airframes with consumables and relations needed for mapping.
        var airframes = db.Airframes
                          .Include(a => a.ModelRef)
                          .Include(a => a.Consumables)
                          .Include(a => a.MaintenanceActions)
                          .Include(a => a.Sessions)
                          .Include(a => a.Squawks)
                              .ThenInclude(s => s.FailureMode)
                          .AsNoTracking()
                          .ToList();

        return airframes.Select(a => MapAirframe(a, repo)).ToList();
    }

    private IReadOnlyList<SquawkVm> LoadSquawksSync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();

        var squawks = db.Squawks
                        .Include(s => s.Airframe)
                        .AsNoTracking()
                        .ToList();

        return squawks.Select(MapSquawk).ToList();
    }

    // ── Mapping helpers ──────────────────────────────────────────────────

    private AirframeVm MapAirframe(Entities.Airframe a, IFleetRepository repo)
    {
        var consumables = BuildConsumables(a.Consumables);

        // Derive HobbsSinceMx from the latest MaintenanceAction
        var hobbsSinceMx = ComputeHobbsSinceMx(a.MaintenanceActions, a.TotalHobbsHours);

        // NextInspectionHrs: hardcoded 100h interval minus HobbsSinceMx, clamped to >=0
        // TODO: proper version pulls per-airframe interval from ComponentTemplate or maintenance program
        var nextInspectionHrs = Math.Max(0, 100.0 - hobbsSinceMx);

        // Count open and deferred squawks
        var openCount = a.Squawks.Count(s => s.Status == (int)SquawkStatus.Open);
        var deferredCount = a.Squawks.Count(s => s.Status == (int)SquawkStatus.Deferred);

        // Determine Status based on squawks and inspection window
        var status = ComputeAirframeStatus(a.Squawks, nextInspectionHrs);

        // Derive LastFlight from the most recent Session
        var lastFlight = ComputeLastFlight(a.Sessions);

        return new AirframeVm
        {
            Id               = a.Id.ToString(),
            Tail             = a.Tail,
            Type             = a.Type,
            Model            = a.ModelRef?.Name ?? a.Type,
            // TODO: Nickname from future DB column
            Nickname         = string.Empty,
            Status           = status,
            Hours            = a.TotalHobbsHours,
            Cycles           = a.TotalCycles,
            HobbsSinceMx     = hobbsSinceMx,
            NextInspectionHrs = nextInspectionHrs,
            OpenSquawks      = openCount,
            Deferred         = deferredCount,
            Consumables      = consumables,
            LastFlight       = lastFlight,
            // TODO: once SessionService exists, set Live = airframe has open session.
            Live             = false,
            LiveState        = null,
        };
    }

    private static double ComputeHobbsSinceMx(List<Entities.MaintenanceAction> actions, double totalHobbs)
    {
        if (actions.Count == 0)
            return 0.0;

        var latest = actions.OrderByDescending(m => m.PerformedAt).First();
        return Math.Max(0, totalHobbs - latest.HoursAtAction);
    }

    private static AirframeStatus ComputeAirframeStatus(List<Entities.Squawk> squawks, double nextInspectionHrs)
    {
        // Check for grounding squawks: open squawks whose FailureMode has Grounding severity
        if (squawks.Any(s => s.Status == (int)SquawkStatus.Open &&
                            s.FailureMode?.Severity == (int)FailureSeverity.Grounding))
            return AirframeStatus.Grounded;

        // Check for any open squawks (non-deferred)
        if (squawks.Any(s => s.Status == (int)SquawkStatus.Open))
            return AirframeStatus.Squawks;

        // Check if maintenance is due (NextInspectionHrs < 5)
        if (nextInspectionHrs < 5)
            return AirframeStatus.MxDue;

        return AirframeStatus.Airworthy;
    }

    private static LastFlightVm ComputeLastFlight(List<Entities.Session> sessions)
    {
        if (sessions.Count == 0)
            return new LastFlightVm("—", "—", 0, 0, 0);

        var latest = sessions.OrderByDescending(s => s.EndedAt ?? s.StartedAt).First();

        // Format date
        var date = (latest.EndedAt ?? latest.StartedAt).ToString("yyyy-MM-dd");

        // Compute duration: (EndedAt - StartedAt).TotalMinutes formatted H:mm
        var duration = "—";
        if (latest.EndedAt.HasValue)
        {
            var totalMinutes = (latest.EndedAt.Value - latest.StartedAt).TotalMinutes;
            var hours = (int)(totalMinutes / 60);
            var minutes = (int)(totalMinutes % 60);
            duration = $"{hours}:{minutes:D2}";
        }

        // Parse OvertempEventsJson to count overtemps
        int overtemps = 0;
        try
        {
            using var doc = JsonDocument.Parse(latest.OvertempEventsJson);
            overtemps = doc.RootElement.GetArrayLength();
        }
        catch
        {
            overtemps = 0;
        }

        return new LastFlightVm(
            Date: date,
            Duration: duration,
            MaxG: latest.MaxG,
            HardLandings: latest.HardLandings,
            Overtemps: overtemps
        );
    }

    private static ConsumablesVm BuildConsumables(List<Entities.Consumable> consumables)
    {
        double Get(int kind) =>
            consumables.FirstOrDefault(c => c.Kind == kind)?.Level ?? 0.0;

        return new ConsumablesVm(
            Oil:      Get(0),
            Tires:    Get(1),
            Brakes:   Get(2),
            Battery:  Get(3),
            Hydraulic: Get(4));
    }

    private static SquawkVm MapSquawk(Entities.Squawk s)
    {
        // Until the Squawk entity gains Component + Summary columns, we read
        // these from the Notes field where the seeder packed them.
        // TODO: add Component + Summary columns in a future migration.
        var severity = s.Status switch
        {
            1 => SquawkSeverity.Deferred,
            2 => SquawkSeverity.Grounding,
            _ => SquawkSeverity.Open,
        };

        // Parse component from notes prefix "…. Component: X. …"
        var component = ExtractNoteTag(s.Notes, "Component") ?? "Unknown";
        var melStr    = ExtractNoteTag(s.Notes, "MEL-deferrable");
        var melDef    = melStr?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        // Strip the appended tags from display notes
        var notes = StripNoteTags(s.Notes);

        return new SquawkVm
        {
            Id           = s.Id.ToString()[..8].ToUpperInvariant(),  // short display ID
            Tail         = s.Airframe?.Tail ?? string.Empty,
            Component    = component,
            Summary      = notes.Length > 80 ? notes[..80] + "…" : notes,
            Severity     = severity,
            MelDeferrable = melDef,
            Opened       = s.Opened.ToString("yyyy-MM-dd"),
            HoursAtOpen  = s.HoursAtOpen,
            DeferredUntil = s.DeferredUntil?.ToString("yyyy-MM-dd"),
            Notes        = notes,
        };
    }

    private static string? ExtractNoteTag(string notes, string tag)
    {
        if (tag == "Component")
        {
            var idx = notes.IndexOf("Component: ", StringComparison.Ordinal);
            if (idx < 0) return null;
            var start = idx + "Component: ".Length;
            var end   = notes.IndexOf('.', start);
            return end > start ? notes[start..end].Trim() : null;
        }
        if (tag == "MEL-deferrable")
        {
            var idx = notes.IndexOf("MEL-deferrable: ", StringComparison.Ordinal);
            if (idx < 0) return null;
            var start = idx + "MEL-deferrable: ".Length;
            return notes[start..].Trim();
        }
        return null;
    }

    private static string StripNoteTags(string notes)
    {
        // Notes format from seeder: "<summary>. Component: X. <rest>. MEL-deferrable: Y"
        // The first sentence is the summary; the structured tags follow.
        var melIdx = notes.IndexOf(" MEL-deferrable:", StringComparison.Ordinal);
        if (melIdx > 0) notes = notes[..melIdx].Trim();

        var compIdx = notes.IndexOf(" Component:", StringComparison.Ordinal);
        if (compIdx > 0) notes = notes[..compIdx].Trim();

        return notes.TrimEnd('.').Trim();
    }
}

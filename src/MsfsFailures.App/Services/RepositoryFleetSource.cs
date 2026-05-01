// TODO (future passes — synthetic / cosmetic fields not yet populated from DB):
//   - AirframeVm.Nickname       → populate from a future airframes.nickname column or separate metadata table
//   - AirframeVm.HobbsSinceMx   → derive from last MaintenanceAction.PerformedAt + elapsed Hobbs hours
//   - AirframeVm.NextInspectionHrs → derive from inspection_interval_hours - HobbsSinceMx
//   - AirframeVm.OpenSquawks / Deferred → aggregate from Squawks table (currently zeroed)
//   - AirframeVm.LastFlight      → query Sessions table for most recent session per airframe
//   - AirframeVm.Status          → derive from squawk severity + inspection window; currently forced Airworthy
//   - AirframeVm.Live / LiveState→ no Live flag in Airframe entity yet; will come from a live Session row
//                                  N350KA "live" tinting will not appear until a real live-session concept is added.
//   - SquawkVm.Id / Component / Summary → Squawk entity packs these into Notes for now (seed compat);
//                                          a future migration adds Component + Summary columns.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.Data;
using MsfsFailures.Data.Entities;
using MsfsFailures.App.ViewModels;

namespace MsfsFailures.App.Services;

/// <summary>
/// <see cref="IFleetSource"/> backed by <see cref="IFleetRepository"/> (SQLite).
/// Reads on construction (sync-over-async) which is acceptable at app startup
/// after the host has started and migrations/seed have run.
/// </summary>
public sealed class RepositoryFleetSource : IFleetSource
{
    private readonly IReadOnlyList<AirframeVm> _airframes;
    private readonly IReadOnlyList<SquawkVm> _squawks;

    public RepositoryFleetSource(IServiceScopeFactory scopeFactory)
    {
        // Open a fresh scope so we get a Scoped FleetDbContext (DbContext is Scoped).
        using var scope = scopeFactory.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<FleetDbContext>();

        // Include Consumables so BuildConsumables has data; GetAllAirframesAsync
        // only includes ModelRef, so we go directly to db for a fuller projection.
        var airframes = db.Airframes
                          .Include(a => a.ModelRef)
                          .Include(a => a.Consumables)
                          .AsNoTracking()
                          .ToList();
        var squawks   = db.Squawks
                          .Include(s => s.Airframe)
                          .AsNoTracking()
                          .ToList();

        _airframes = airframes.Select(MapAirframe).ToList();
        _squawks   = squawks.Select(MapSquawk).ToList();
    }

    public IReadOnlyList<AirframeVm> GetAirframes() => _airframes;
    public IReadOnlyList<SquawkVm>   GetSquawks()   => _squawks;

    // ── Mapping helpers ──────────────────────────────────────────────────

    private static AirframeVm MapAirframe(Airframe a)
    {
        var consumables = BuildConsumables(a.Consumables);
        return new AirframeVm
        {
            Id               = a.Id.ToString(),
            Tail             = a.Tail,
            Type             = a.Type,
            Model            = a.ModelRef?.Name ?? a.Type,
            // TODO: Nickname from future DB column
            Nickname         = string.Empty,
            // TODO: Status derived from squawks + inspection window
            Status           = AirframeStatus.Airworthy,
            Hours            = a.TotalHobbsHours,
            Cycles           = a.TotalCycles,
            // TODO: HobbsSinceMx from last MaintenanceAction
            HobbsSinceMx     = 0.0,
            // TODO: NextInspectionHrs from inspection interval
            NextInspectionHrs = 100.0,
            // TODO: OpenSquawks + Deferred from Squawks table aggregate
            OpenSquawks      = 0,
            Deferred         = 0,
            Consumables      = consumables,
            // TODO: LastFlight from Sessions table
            LastFlight       = new LastFlightVm("—", "—", 0, 0, 0),
            // TODO: Live from a live Session row; N350KA tinting deferred
            Live             = false,
            LiveState        = null,
        };
    }

    private static ConsumablesVm BuildConsumables(List<Consumable> consumables)
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

    private static SquawkVm MapSquawk(Squawk s)
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

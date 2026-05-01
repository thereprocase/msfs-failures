using Microsoft.EntityFrameworkCore;
using MsfsFailures.Core;
using Entities = MsfsFailures.Data.Entities;
using CoreSquawkStatus = MsfsFailures.Core.SquawkStatus;

namespace MsfsFailures.Data.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFleetRepository"/>.
/// Registered as Scoped (one per DI scope / request) so it shares the
/// Scoped <see cref="FleetDbContext"/> and change-tracking is consistent
/// within a unit of work. For a single-user desktop app one scope per
/// background tick is fine; the host can open/close scopes as needed.
/// </summary>
public sealed class FleetRepository : IFleetRepository
{
    private readonly FleetDbContext _db;

    public FleetRepository(FleetDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Entities.Airframe>> GetAllAirframesAsync(CancellationToken ct = default)
    {
        return await _db.Airframes
            .Include(a => a.ModelRef)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Entities.Airframe?> GetAirframeAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Airframes
            .Include(a => a.ModelRef)
            .Include(a => a.Components).ThenInclude(c => c.Template)
            .Include(a => a.Consumables)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddSessionAsync(Entities.Session session, CancellationToken ct = default)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.Squawk>> GetOpenSquawksAsync(Guid airframeId, CancellationToken ct = default)
    {
        // Status == 0 maps to Core.SquawkStatus.Open (int enum, no coupling needed here)
        return await _db.Squawks
            .Where(s => s.AirframeId == airframeId && s.Status == 0)
            .Include(s => s.FailureMode)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ── v1 tick-loop extensions ──────────────────────────────────────────────

    public async Task<Entities.Airframe?> GetAirframeByTailAsync(string tail, CancellationToken ct = default)
    {
        return await _db.Airframes
            .Include(a => a.ModelRef)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Tail == tail, ct);
    }

    public async Task<IReadOnlyList<Entities.Component>> GetComponentsForAirframeAsync(Guid airframeId, CancellationToken ct = default)
    {
        return await _db.Components
            .Where(c => c.AirframeId == airframeId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.ComponentTemplate>> GetTemplatesForModelAsync(Guid modelRefId, CancellationToken ct = default)
    {
        return await _db.ComponentTemplates
            .Where(t => t.ModelRefId == modelRefId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.FailureMode>> GetFailureModesForTemplatesAsync(
        IReadOnlyList<Guid> templateIds, CancellationToken ct = default)
    {
        return await _db.FailureModes
            .Where(f => templateIds.Contains(f.TemplateId))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.Accelerator>> GetAcceleratorsAsync(CancellationToken ct = default)
    {
        return await _db.Accelerators
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.Consumable>> GetConsumablesForAirframeAsync(Guid airframeId, CancellationToken ct = default)
    {
        return await _db.Consumables
            .Where(c => c.AirframeId == airframeId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task ApplyTickResultAsync(
        Guid airframeId,
        double hobbsHoursDelta,
        int cyclesDelta,
        IReadOnlyDictionary<Guid, double> componentWear,
        IReadOnlyDictionary<Guid, double> consumableLevels,
        CancellationToken ct = default)
    {
        // Increment airframe cumulative counters
        var airframe = await _db.Airframes.FindAsync([airframeId], ct);
        if (airframe is not null)
        {
            airframe.TotalHobbsHours += hobbsHoursDelta;
            airframe.TotalCycles     += cyclesDelta;
        }

        // Apply component wear deltas — clamp to [0..1]
        if (componentWear.Count > 0)
        {
            var componentIds = componentWear.Keys.ToList();
            var components = await _db.Components
                .Where(c => componentIds.Contains(c.Id))
                .ToListAsync(ct);
            foreach (var component in components)
            {
                if (componentWear.TryGetValue(component.Id, out double delta))
                    component.Wear = Math.Clamp(component.Wear + delta, 0.0, 1.0);
            }
        }

        // Apply consumable level deltas — clamp to [0..1]
        if (consumableLevels.Count > 0)
        {
            var consumableIds = consumableLevels.Keys.ToList();
            var consumables = await _db.Consumables
                .Where(c => consumableIds.Contains(c.Id))
                .ToListAsync(ct);
            foreach (var consumable in consumables)
            {
                if (consumableLevels.TryGetValue(consumable.Id, out double delta))
                    consumable.Level = Math.Clamp(consumable.Level + delta, 0.0, 1.0);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<Entities.Session> StartSessionAsync(Guid airframeId, double hobbsAtStart, CancellationToken ct = default)
    {
        var session = new Entities.Session
        {
            Id               = Guid.NewGuid(),
            AirframeId       = airframeId,
            StartedAt        = DateTimeOffset.UtcNow,
            EndedAt          = null,
            HobbsStart       = hobbsAtStart,
            HobbsEnd         = null,
            MaxG             = 1.0,
            HardLandings     = 0,
            OvertempEventsJson = "[]",
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task EndSessionAsync(
        Guid sessionId, double hobbsAtEnd, double maxG, int hardLandings, CancellationToken ct = default)
    {
        var session = await _db.Sessions.FindAsync([sessionId], ct);
        if (session is null) return;

        session.EndedAt      = DateTimeOffset.UtcNow;
        session.HobbsEnd     = hobbsAtEnd;
        session.MaxG         = maxG;
        session.HardLandings = hardLandings;

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.MaintenanceAction>> GetMaintenanceActionsForAirframeAsync(
        Guid airframeId, CancellationToken ct = default)
    {
        // SQLite can't ORDER BY DateTimeOffset — sort client-side after pulling.
        var actions = await _db.MaintenanceActions
            .Where(m => m.AirframeId == airframeId)
            .AsNoTracking()
            .ToListAsync(ct);
        return actions.OrderByDescending(m => m.PerformedAt).ToList();
    }

    public async Task<Entities.Session?> GetMostRecentSessionAsync(Guid airframeId, CancellationToken ct = default)
    {
        // SQLite can't ORDER BY DateTimeOffset — sort client-side after pulling.
        var sessions = await _db.Sessions
            .Where(s => s.AirframeId == airframeId)
            .AsNoTracking()
            .ToListAsync(ct);
        return sessions
            .OrderByDescending(s => s.EndedAt ?? s.StartedAt)
            .FirstOrDefault();
    }

    public async Task<int> CountSquawksAsync(Guid airframeId, CoreSquawkStatus status, CancellationToken ct = default)
    {
        return await _db.Squawks
            .Where(s => s.AirframeId == airframeId && s.Status == (int)status)
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.Squawk>> GetSquawksForAirframeAsync(Guid airframeId, CancellationToken ct = default)
    {
        return await _db.Squawks
            .Where(s => s.AirframeId == airframeId)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Guid?> GetAirframeIdWithOpenSessionAsync(CancellationToken ct = default)
    {
        // SQLite can't ORDER BY DateTimeOffset — sort client-side after pulling.
        var open = await _db.Sessions
            .Where(s => s.EndedAt == null)
            .AsNoTracking()
            .ToListAsync(ct);
        return open
            .OrderByDescending(s => s.StartedAt)
            .Select(s => (Guid?)s.AirframeId)
            .FirstOrDefault();
    }

    public async Task<IReadOnlyList<Entities.ModelRef>> GetAllModelRefsAsync(CancellationToken ct = default)
    {
        return await _db.ModelRefs
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Entities.Airframe?> GetAirframeByModelRefAsync(Guid modelRefId, CancellationToken ct = default)
    {
        return await _db.Airframes
            .Where(a => a.ModelRefId == modelRefId)
            .Include(a => a.ModelRef)
            .AsNoTracking()
            .OrderByDescending(a => a.TotalHobbsHours)
            .FirstOrDefaultAsync(ct);
    }

    // ── Squawk write operations ──────────────────────────────────────────────

    public async Task<Guid> AddSquawkAsync(
        Guid airframeId,
        string component,
        string summary,
        int severity,
        bool melDeferrable,
        string notes,
        double hoursAtOpen,
        CancellationToken ct = default)
    {
        // Pack component/MEL info into the Notes field (same format as seeder).
        var packed = BuildPackedNotes(summary, component, melDeferrable, notes);

        var squawk = new Entities.Squawk
        {
            Id          = Guid.NewGuid(),
            AirframeId  = airframeId,
            Opened      = DateTimeOffset.UtcNow,
            Status      = severity,
            Notes       = packed,
            HoursAtOpen = hoursAtOpen,
        };
        _db.Squawks.Add(squawk);
        await _db.SaveChangesAsync(ct);
        return squawk.Id;
    }

    public async Task UpdateSquawkAsync(
        Guid squawkId,
        string? component,
        string? summary,
        string? notes,
        int? status,
        DateTimeOffset? deferredUntil,
        CancellationToken ct = default)
    {
        var squawk = await _db.Squawks.FindAsync([squawkId], ct);
        if (squawk is null) return;

        // Reconstruct packed notes when any field changes.
        if (component != null || summary != null || notes != null)
        {
            // Unpack existing values to fill any unchanged fields.
            var existingComponent = UnpackTag(squawk.Notes, "Component") ?? string.Empty;
            var existingMelStr    = UnpackTag(squawk.Notes, "MEL-deferrable");
            var existingMel       = existingMelStr?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            var existingNotes     = StripPackedTags(squawk.Notes);

            var newComponent = component ?? existingComponent;
            var newNotes     = notes     ?? existingNotes;
            var newSummary   = summary   ?? existingNotes; // summary lives as the first sentence

            squawk.Notes = BuildPackedNotes(newSummary, newComponent, existingMel, newNotes);
        }

        if (status != null)
            squawk.Status = status.Value;

        if (deferredUntil != null)
            squawk.DeferredUntil = deferredUntil.Value;

        _db.Squawks.Update(squawk);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteSquawkAsync(Guid squawkId, CancellationToken ct = default)
    {
        var squawk = await _db.Squawks.FindAsync([squawkId], ct);
        if (squawk is null) return;
        _db.Squawks.Remove(squawk);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Entities.Squawk>> GetAllSquawksAsync(CancellationToken ct = default)
    {
        return await _db.Squawks
            .Include(s => s.Airframe)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ── Private pack/unpack helpers ──────────────────────────────────────────

    private static string BuildPackedNotes(string summary, string component, bool melDeferrable, string notes)
    {
        // Format: "<summary>. Component: <component>. <notes>. MEL-deferrable: <true|false>"
        var sb = new System.Text.StringBuilder();
        sb.Append(summary.TrimEnd('.'));
        sb.Append(". Component: ");
        sb.Append(component);
        sb.Append(". ");
        sb.Append(notes.TrimEnd('.'));
        sb.Append(". MEL-deferrable: ");
        sb.Append(melDeferrable ? "true" : "false");
        return sb.ToString();
    }

    private static string? UnpackTag(string notes, string tag)
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

    private static string StripPackedTags(string notes)
    {
        var melIdx = notes.IndexOf(" MEL-deferrable:", StringComparison.Ordinal);
        if (melIdx > 0) notes = notes[..melIdx].Trim();
        var compIdx = notes.IndexOf(" Component:", StringComparison.Ordinal);
        if (compIdx > 0) notes = notes[..compIdx].Trim();
        return notes.TrimEnd('.').Trim();
    }
}

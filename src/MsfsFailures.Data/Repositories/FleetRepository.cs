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
        return await _db.MaintenanceActions
            .Where(m => m.AirframeId == airframeId)
            .OrderByDescending(m => m.PerformedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Entities.Session?> GetMostRecentSessionAsync(Guid airframeId, CancellationToken ct = default)
    {
        return await _db.Sessions
            .Where(s => s.AirframeId == airframeId)
            .OrderByDescending(s => s.EndedAt ?? s.StartedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
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
}

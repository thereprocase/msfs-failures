using Microsoft.EntityFrameworkCore;
using MsfsFailures.Data.Entities;

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

    public async Task<IReadOnlyList<Airframe>> GetAllAirframesAsync(CancellationToken ct = default)
    {
        return await _db.Airframes
            .Include(a => a.ModelRef)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Airframe?> GetAirframeAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Airframes
            .Include(a => a.ModelRef)
            .Include(a => a.Components).ThenInclude(c => c.Template)
            .Include(a => a.Consumables)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddSessionAsync(Session session, CancellationToken ct = default)
    {
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Squawk>> GetOpenSquawksAsync(Guid airframeId, CancellationToken ct = default)
    {
        // Status == 0 maps to Core.SquawkStatus.Open (int enum, no coupling needed here)
        return await _db.Squawks
            .Where(s => s.AirframeId == airframeId && s.Status == 0)
            .Include(s => s.FailureMode)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}

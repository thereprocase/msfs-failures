using MsfsFailures.Data.Entities;

namespace MsfsFailures.Data.Repositories;

/// <summary>
/// Minimum repository surface needed for v1 fleet operations.
/// </summary>
public interface IFleetRepository
{
    Task<IReadOnlyList<Airframe>> GetAllAirframesAsync(CancellationToken ct = default);
    Task<Airframe?> GetAirframeAsync(Guid id, CancellationToken ct = default);
    Task AddSessionAsync(Session session, CancellationToken ct = default);
    Task<IReadOnlyList<Squawk>> GetOpenSquawksAsync(Guid airframeId, CancellationToken ct = default);
}

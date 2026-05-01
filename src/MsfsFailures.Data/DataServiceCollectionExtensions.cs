using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MsfsFailures.Data.Repositories;

namespace MsfsFailures.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="FleetDbContext"/> and <see cref="IFleetRepository"/> with the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="dbPath">Absolute path to the SQLite database file (e.g. fleet.db in %LOCALAPPDATA%\MsfsFailures).</param>
    /// <remarks>
    /// Does NOT call <c>Database.Migrate()</c> — that is the host bootstrap agent's responsibility.
    /// DbContext is Scoped; FleetRepository is Scoped so it shares the context within one unit of work.
    /// </remarks>
    public static IServiceCollection AddMsfsFailuresData(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<FleetDbContext>(o =>
            o.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IFleetRepository, FleetRepository>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MsfsFailures.Data;

/// <summary>
/// Used by <c>dotnet ef migrations add</c> at design time only.
/// Points at a local fleet.db in the working directory — never used at runtime.
/// </summary>
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<FleetDbContext>
{
    public FleetDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<FleetDbContext>()
            .UseSqlite("Data Source=fleet.db")
            .Options;
        return new FleetDbContext(opts);
    }
}

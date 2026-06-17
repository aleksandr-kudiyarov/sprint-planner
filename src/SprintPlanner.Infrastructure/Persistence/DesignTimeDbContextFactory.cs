using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SprintPlanner.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context at design time (migrations) without booting
/// the API host. The connection string here is only used to resolve the Npgsql provider;
/// no database connection is opened to scaffold a migration.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SPRINTPLANNER_DB")
            ?? "Host=localhost;Port=5432;Database=sprintplanner;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connection)
            .Options;

        return new AppDbContext(options);
    }
}

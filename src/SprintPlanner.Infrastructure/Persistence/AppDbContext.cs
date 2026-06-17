using Microsoft.EntityFrameworkCore;
using SprintPlanner.Application.Abstractions;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Domain.Entities;

namespace SprintPlanner.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Developer> Developers => Set<Developer>();
    public DbSet<CalendarOverride> CalendarOverrides => Set<CalendarOverride>();
    public DbSet<Competency> Competencies => Set<Competency>();
    public DbSet<DeveloperCompetency> DeveloperCompetencies => Set<DeveloperCompetency>();
    public DbSet<DeveloperCompetencyHistory> DeveloperCompetencyHistory => Set<DeveloperCompetencyHistory>();
    public DbSet<BacklogItem> BacklogItems => Set<BacklogItem>();
    public DbSet<CompetencyRequirement> CompetencyRequirements => Set<CompetencyRequirement>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<SprintPlan> SprintPlans => Set<SprintPlan>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<SprintHistory> SprintHistory => Set<SprintHistory>();
    public DbSet<GlobalSettings> GlobalSettings => Set<GlobalSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Ids are always assigned in code (see Entity), so keep the database out of key
        // generation. Without this, Npgsql treats Guid keys as store-generated and EF
        // mistakes navigation-discovered children for existing rows (UPDATE → 0 rows).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var key = entity.FindProperty("Id");
            if (key is not null && key.ClrType == typeof(Guid))
            {
                key.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }

        // Seed the singleton settings row with defaults.
        modelBuilder.Entity<GlobalSettings>().HasData(new GlobalSettings());
    }
}

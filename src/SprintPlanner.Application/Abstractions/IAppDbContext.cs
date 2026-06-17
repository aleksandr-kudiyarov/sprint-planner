using Microsoft.EntityFrameworkCore;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Domain.Entities;

namespace SprintPlanner.Application.Abstractions;

/// <summary>
/// Abstraction over the EF Core context so application services depend on the domain
/// sets, not on the concrete Infrastructure context.
/// </summary>
public interface IAppDbContext
{
    DbSet<Developer> Developers { get; }
    DbSet<CalendarOverride> CalendarOverrides { get; }
    DbSet<Competency> Competencies { get; }
    DbSet<DeveloperCompetency> DeveloperCompetencies { get; }
    DbSet<DeveloperCompetencyHistory> DeveloperCompetencyHistory { get; }
    DbSet<BacklogItem> BacklogItems { get; }
    DbSet<CompetencyRequirement> CompetencyRequirements { get; }
    DbSet<Sprint> Sprints { get; }
    DbSet<SprintPlan> SprintPlans { get; }
    DbSet<TaskAssignment> TaskAssignments { get; }
    DbSet<SprintHistory> SprintHistory { get; }
    DbSet<GlobalSettings> GlobalSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

using SprintPlanner.Domain.Common;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A skill requirement attached to a backlog item: a developer must own
/// <see cref="CompetencyId"/> at <see cref="MinimumLevel"/> or higher to be eligible.
/// </summary>
public class CompetencyRequirement : Entity
{
    public Guid BacklogItemId { get; set; }
    public Guid CompetencyId { get; set; }
    public CompetencyLevel MinimumLevel { get; set; } = CompetencyLevel.Junior;

    public Competency? Competency { get; set; }
}

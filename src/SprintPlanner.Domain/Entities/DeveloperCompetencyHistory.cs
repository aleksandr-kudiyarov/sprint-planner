using SprintPlanner.Domain.Common;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// Append-only record of a change to a developer's competency level (F1.2:
/// "История изменений уровней сохраняется с датой и автором").
/// </summary>
public class DeveloperCompetencyHistory : Entity
{
    public Guid DeveloperId { get; set; }
    public Guid CompetencyId { get; set; }
    public CompetencyLevel FromLevel { get; set; }
    public CompetencyLevel ToLevel { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string ChangedBy { get; set; } = string.Empty;
}

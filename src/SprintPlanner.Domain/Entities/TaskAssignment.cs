using SprintPlanner.Domain.Common;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A single task-to-developer assignment within a plan variant.
/// </summary>
public class TaskAssignment : Entity
{
    public Guid SprintPlanId { get; set; }
    public Guid TaskId { get; set; }
    public Guid DeveloperId { get; set; }

    /// <summary>Raw estimate copied from the backlog item at generation time.</summary>
    public decimal EstimatedHours { get; set; }

    /// <summary>Estimate after applying the developer's β correction factor.</summary>
    public decimal AdjustedHours { get; set; }

    /// <summary>True when the assignee matches the task's preferred/last developer.</summary>
    public bool ContinuityBonus { get; set; }

    /// <summary>Skill-fit score for this assignment, 0.0 .. 1.0.</summary>
    public decimal SkillFitScore { get; set; }

    /// <summary>True when this task lies on the plan's dependency critical path.</summary>
    public bool IsOnCriticalPath { get; set; }
}

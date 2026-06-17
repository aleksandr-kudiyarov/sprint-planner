using SprintPlanner.Domain.Enums;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Application.Planning;

/// <summary>
/// A developer as seen by the solver: resolved capacity, β correction, and skill levels.
/// </summary>
public sealed class PlanningDeveloper
{
    public required Guid DeveloperId { get; init; }
    public required string Name { get; init; }

    /// <summary>Effective capacity over the sprint window, in hours (F1.1).</summary>
    public required decimal EffectiveCapacity { get; init; }

    /// <summary>Overall β estimate-accuracy factor.</summary>
    public decimal Beta { get; init; } = 1.0m;

    /// <summary>β per task type; falls back to <see cref="Beta"/> when a type is missing.</summary>
    public IReadOnlyDictionary<BacklogItemType, decimal> BetaByType { get; init; }
        = new Dictionary<BacklogItemType, decimal>();

    /// <summary>Owned level per competency id (absent ⇒ <see cref="CompetencyLevel.None"/>).</summary>
    public IReadOnlyDictionary<Guid, CompetencyLevel> SkillLevels { get; init; }
        = new Dictionary<Guid, CompetencyLevel>();

    /// <summary>Numeric [0..1] proficiency per competency id, for skill-fit scoring.</summary>
    public IReadOnlyDictionary<Guid, decimal> SkillNumeric { get; init; }
        = new Dictionary<Guid, decimal>();

    public decimal BetaFor(BacklogItemType type)
        => BetaByType.TryGetValue(type, out var b) ? b : Beta;

    public CompetencyLevel LevelOf(Guid competencyId)
        => SkillLevels.TryGetValue(competencyId, out var lvl) ? lvl : CompetencyLevel.None;

    public decimal NumericOf(Guid competencyId)
        => SkillNumeric.TryGetValue(competencyId, out var n) ? n : LevelOf(competencyId).ToNumeric();
}

/// <summary>A task as seen by the solver, with uncertainty σ already resolved.</summary>
public sealed class PlanningTask
{
    public required Guid TaskId { get; init; }
    public required string Title { get; init; }
    public BacklogItemType Type { get; init; }
    public int Priority { get; init; }
    public decimal BusinessValue { get; init; }
    public decimal EstimatedHours { get; init; }

    /// <summary>Estimate standard deviation σ (F3.4).</summary>
    public decimal Sigma { get; init; }

    /// <summary>competencyId → minimum required level.</summary>
    public IReadOnlyDictionary<Guid, CompetencyLevel> RequiredCompetencies { get; init; }
        = new Dictionary<Guid, CompetencyLevel>();

    /// <summary>Ids of tasks (within this problem) that must be included before this one.</summary>
    public IReadOnlyList<Guid> DependencyIds { get; init; } = Array.Empty<Guid>();

    public Guid? LockedDeveloperId { get; init; }
    public Guid? PreferredDeveloperId { get; init; }
}

/// <summary>
/// A complete, solver-ready planning problem. Produced by <c>PlanningProblemBuilder</c>
/// from domain entities + statistics + settings.
/// </summary>
public sealed class PlanningProblem
{
    public required IReadOnlyList<PlanningDeveloper> Developers { get; init; }
    public required IReadOnlyList<PlanningTask> Tasks { get; init; }
    public required OptimizationWeights Weights { get; init; }

    /// <summary>
    /// Robustness buffer Γ applied to aggregated σ in the capacity constraint.
    /// Derived from the TL's risk appetite (higher appetite ⇒ smaller Γ).
    /// </summary>
    public decimal Gamma { get; init; }

    /// <summary>Solver wall-clock limit per generation, seconds (F3.2).</summary>
    public double SolverTimeLimitSeconds { get; init; } = 10.0;

    /// <summary>
    /// True when this developer is eligible for this task: every required competency is
    /// owned at or above its minimum level.
    /// </summary>
    public bool IsEligible(PlanningDeveloper dev, PlanningTask task)
        => task.RequiredCompetencies.All(req => dev.LevelOf(req.Key).Satisfies(req.Value));

    /// <summary>
    /// Skill-fit score (0..1): mean numeric proficiency across the task's required
    /// competencies. Tasks with no requirements score 1.0.
    /// </summary>
    public decimal SkillFit(PlanningDeveloper dev, PlanningTask task)
    {
        if (task.RequiredCompetencies.Count == 0)
        {
            return 1.0m;
        }

        return task.RequiredCompetencies.Average(req => dev.NumericOf(req.Key));
    }

    /// <summary>Adjusted hours for a (dev, task) pair: estimate × the developer's β for the type.</summary>
    public decimal AdjustedHours(PlanningDeveloper dev, PlanningTask task)
        => task.EstimatedHours * dev.BetaFor(task.Type);
}

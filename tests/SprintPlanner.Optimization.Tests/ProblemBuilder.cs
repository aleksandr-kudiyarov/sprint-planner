using SprintPlanner.Application.Planning;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Optimization.Tests;

/// <summary>Fluent helpers for assembling <see cref="PlanningProblem"/> fixtures in tests.</summary>
internal sealed class ProblemBuilder
{
    private readonly List<PlanningDeveloper> _developers = new();
    private readonly List<PlanningTask> _tasks = new();
    private OptimizationWeights _weights = OptimizationWeights.Balance;
    private decimal _gamma;

    public ProblemBuilder Weights(OptimizationWeights weights)
    {
        _weights = weights;
        return this;
    }

    public ProblemBuilder Gamma(decimal gamma)
    {
        _gamma = gamma;
        return this;
    }

    public PlanningDeveloper AddDeveloper(
        string name,
        decimal capacity,
        decimal beta = 1.0m,
        (Guid Competency, CompetencyLevel Level)[]? skills = null)
    {
        var levels = (skills ?? Array.Empty<(Guid, CompetencyLevel)>())
            .ToDictionary(s => s.Competency, s => s.Level);
        var numeric = levels.ToDictionary(kv => kv.Key, kv => kv.Value.ToNumeric());

        var dev = new PlanningDeveloper
        {
            DeveloperId = Guid.NewGuid(),
            Name = name,
            EffectiveCapacity = capacity,
            Beta = beta,
            SkillLevels = levels,
            SkillNumeric = numeric
        };
        _developers.Add(dev);
        return dev;
    }

    public PlanningTask AddTask(
        string title,
        decimal estimatedHours,
        decimal businessValue = 1m,
        decimal sigma = 0m,
        (Guid Competency, CompetencyLevel Min)[]? requires = null,
        Guid[]? dependsOn = null,
        Guid? lockedDeveloperId = null,
        Guid? preferredDeveloperId = null)
    {
        var task = new PlanningTask
        {
            TaskId = Guid.NewGuid(),
            Title = title,
            EstimatedHours = estimatedHours,
            BusinessValue = businessValue,
            Sigma = sigma,
            RequiredCompetencies = (requires ?? Array.Empty<(Guid, CompetencyLevel)>())
                .ToDictionary(r => r.Competency, r => r.Min),
            DependencyIds = dependsOn ?? Array.Empty<Guid>(),
            LockedDeveloperId = lockedDeveloperId,
            PreferredDeveloperId = preferredDeveloperId
        };
        _tasks.Add(task);
        return task;
    }

    public PlanningProblem Build() => new()
    {
        Developers = _developers,
        Tasks = _tasks,
        Weights = _weights,
        Gamma = _gamma,
        SolverTimeLimitSeconds = 5.0
    };
}

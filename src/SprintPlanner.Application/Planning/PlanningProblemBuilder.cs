using SprintPlanner.Application.Configuration;
using SprintPlanner.Application.Statistics;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Application.Planning;

/// <summary>Inputs needed to assemble a <see cref="PlanningProblem"/>.</summary>
public sealed class PlanningProblemRequest
{
    public required DateOnly SprintStart { get; init; }
    public required DateOnly SprintEnd { get; init; }
    public required IReadOnlyList<Developer> Developers { get; init; }
    public required IReadOnlyList<BacklogItem> Tasks { get; init; }
    public required GlobalSettings Settings { get; init; }
    public required OptimizationWeights Weights { get; init; }

    /// <summary>Risk appetite 0.0 (risk-averse) .. 1.0 (risk-seeking); drives Γ.</summary>
    public decimal RiskAppetite { get; init; } = 0.5m;

    /// <summary>Completed-task history for β/σ statistics (empty on cold start).</summary>
    public IReadOnlyList<CompletedTaskRecord> History { get; init; } = Array.Empty<CompletedTaskRecord>();

    /// <summary>Completed sprint ids ordered oldest → newest.</summary>
    public IReadOnlyList<Guid> SprintChronology { get; init; } = Array.Empty<Guid>();
}

/// <summary>
/// Translates domain entities + statistics + settings into the solver-ready
/// <see cref="PlanningProblem"/>. This is the seam between persistence/statistics and the
/// optimizer, keeping the optimizer free of EF and history concerns.
/// </summary>
public sealed class PlanningProblemBuilder
{
    /// <summary>Maximum robustness buffer when the TL is fully risk-averse.</summary>
    public const decimal GammaMax = 2.0m;

    private readonly StatisticsCalculator _statistics;

    public PlanningProblemBuilder(StatisticsCalculator statistics) => _statistics = statistics;

    public PlanningProblem Build(PlanningProblemRequest request)
    {
        var selectedTaskIds = request.Tasks.Select(t => t.Id).ToHashSet();

        var developers = request.Developers
            .Where(d => d.IsActive)
            .Select(d => BuildDeveloper(d, request))
            .ToList();

        var tasks = request.Tasks
            .Select(t => BuildTask(t, request, selectedTaskIds))
            .ToList();

        var appetite = Math.Clamp(request.RiskAppetite, 0m, 1m);
        var gamma = (1m - appetite) * GammaMax;

        return new PlanningProblem
        {
            Developers = developers,
            Tasks = tasks,
            Weights = request.Weights,
            Gamma = gamma,
            SolverTimeLimitSeconds = 10.0
        };
    }

    private PlanningDeveloper BuildDeveloper(Developer dev, PlanningProblemRequest request)
    {
        var stats = _statistics.ComputeForDeveloper(dev.Id, request.History, request.SprintChronology);

        // Effective task capacity = available hours over the sprint × the work-time coefficient.
        var rawCapacity = dev.EffectiveCapacity(request.SprintStart, request.SprintEnd);
        var capacity = rawCapacity * request.Settings.EffectiveWorkCoefficient;

        var levels = dev.Competencies.ToDictionary(c => c.CompetencyId, c => c.Level);
        var numeric = dev.Competencies.ToDictionary(c => c.CompetencyId, c => c.LevelNumeric);

        return new PlanningDeveloper
        {
            DeveloperId = dev.Id,
            Name = dev.Name,
            EffectiveCapacity = capacity,
            Beta = stats.Beta,
            BetaByType = stats.BetaByType,
            SkillLevels = levels,
            SkillNumeric = numeric
        };
    }

    private PlanningTask BuildTask(BacklogItem item, PlanningProblemRequest request, HashSet<Guid> selectedTaskIds)
    {
        var sigma = _statistics.ComputeSigmaForType(request.History, item.Type, item.EstimatedHours);

        // F1.3: the last developer is offered as the preferred (continuity) assignee.
        var preferred = item.PreferredDeveloperId ?? item.LastDeveloperId;

        return new PlanningTask
        {
            TaskId = item.Id,
            Title = item.Title,
            Type = item.Type,
            Priority = item.Priority,
            BusinessValue = item.BusinessValue,
            EstimatedHours = item.EstimatedHours,
            Sigma = sigma,
            RequiredCompetencies = item.RequiredCompetencies.ToDictionary(r => r.CompetencyId, r => r.MinimumLevel),
            // Only keep dependencies that are within the selected planning set.
            DependencyIds = item.DependencyIds.Where(selectedTaskIds.Contains).ToList(),
            LockedDeveloperId = item.LockedDeveloperId,
            PreferredDeveloperId = preferred
        };
    }
}

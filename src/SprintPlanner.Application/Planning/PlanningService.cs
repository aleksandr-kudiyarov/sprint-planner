using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SprintPlanner.Application.Abstractions;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Application.Statistics;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Domain.Services;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Application.Planning;

/// <summary>Raised when a planning request is invalid (e.g. cyclic dependencies).</summary>
public sealed class PlanningValidationException(string message) : Exception(message);

/// <summary>
/// Orchestrates a planning session (F3): load data, validate, build the problem, run the
/// optimizer, persist the generated variants, and handle selection / manual edits.
/// </summary>
public sealed class PlanningService
{
    private readonly IAppDbContext _db;
    private readonly PlanningProblemBuilder _builder;
    private readonly ISprintOptimizer _optimizer;
    private readonly PlanMetricsCalculator _metrics;
    private readonly ISolverMetrics _solverMetrics;

    public PlanningService(
        IAppDbContext db,
        PlanningProblemBuilder builder,
        ISprintOptimizer optimizer,
        PlanMetricsCalculator metrics,
        ISolverMetrics solverMetrics)
    {
        _db = db;
        _builder = builder;
        _optimizer = optimizer;
        _metrics = metrics;
        _solverMetrics = solverMetrics;
    }

    /// <summary>
    /// Generates a fresh Pareto front for a sprint, replacing any previously generated
    /// (non-selected) variants. Returns the persisted plans.
    /// </summary>
    public async Task<IReadOnlyList<SprintPlan>> GeneratePlansAsync(
        Guid sprintId,
        IReadOnlyCollection<Guid> taskIds,
        OptimizationWeights weights,
        decimal riskAppetite,
        CancellationToken ct = default)
    {
        var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == sprintId, ct)
            ?? throw new PlanningValidationException($"Sprint {sprintId} not found.");

        var settings = await GetSettingsAsync(ct);

        var developers = await _db.Developers
            .Include(d => d.Competencies)
            .Include(d => d.CalendarOverrides)
            .Where(d => d.IsActive)
            .ToListAsync(ct);

        var tasks = await _db.BacklogItems
            .Include(b => b.RequiredCompetencies)
            .Where(b => taskIds.Contains(b.Id))
            .ToListAsync(ct);

        if (tasks.Count == 0)
        {
            throw new PlanningValidationException("No tasks selected for planning.");
        }

        ValidateAcyclic(tasks);

        var problem = _builder.Build(new PlanningProblemRequest
        {
            SprintStart = sprint.StartDate,
            SprintEnd = sprint.EndDate,
            Developers = developers,
            Tasks = tasks,
            Settings = settings,
            Weights = weights,
            RiskAppetite = riskAppetite,
            History = await LoadHistoryAsync(ct),
            SprintChronology = await LoadChronologyAsync(ct)
        });

        var stopwatch = Stopwatch.StartNew();
        var plans = _optimizer.Generate(problem);
        stopwatch.Stop();
        _solverMetrics.RecordGeneration(stopwatch.Elapsed.TotalSeconds, plans.Count);

        await ReplaceGeneratedPlansAsync(sprintId, plans, ct);
        return plans;
    }

    /// <summary>
    /// Approves a plan variant (F4.4): marks it selected, clears other selections, sets the
    /// sprint's selected plan and moves its tasks to InSprint — atomically.
    /// </summary>
    public async Task SelectPlanAsync(Guid sprintId, Guid planId, CancellationToken ct = default)
    {
        var sprint = await _db.Sprints.FirstOrDefaultAsync(s => s.Id == sprintId, ct)
            ?? throw new PlanningValidationException($"Sprint {sprintId} not found.");

        var plans = await _db.SprintPlans
            .Include(p => p.Assignments)
            .Where(p => p.SprintId == sprintId)
            .ToListAsync(ct);

        var selected = plans.FirstOrDefault(p => p.Id == planId)
            ?? throw new PlanningValidationException($"Plan {planId} does not belong to sprint {sprintId}.");

        foreach (var plan in plans)
        {
            plan.IsSelected = plan.Id == planId;
        }

        sprint.SelectedPlanId = planId;

        var assignedTaskIds = selected.Assignments.Select(a => a.TaskId).ToHashSet();
        var assignedTasks = await _db.BacklogItems
            .Where(b => assignedTaskIds.Contains(b.Id))
            .ToListAsync(ct);
        foreach (var task in assignedTasks)
        {
            task.Status = BacklogItemStatus.InSprint;
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Reassigns a task within a plan (F4.3 drag &amp; drop) and recomputes the plan's
    /// metrics. Skill/capacity violations are warned about, not blocked. Locked tasks
    /// cannot be moved.
    /// </summary>
    public async Task<SprintPlan> ReassignTaskAsync(
        Guid planId, Guid taskId, Guid newDeveloperId, CancellationToken ct = default)
    {
        var plan = await _db.SprintPlans
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new PlanningValidationException($"Plan {planId} not found.");

        var assignment = plan.Assignments.FirstOrDefault(a => a.TaskId == taskId)
            ?? throw new PlanningValidationException($"Task {taskId} is not part of plan {planId}.");

        var task = await _db.BacklogItems
            .Include(b => b.RequiredCompetencies)
            .FirstOrDefaultAsync(b => b.Id == taskId, ct)
            ?? throw new PlanningValidationException($"Task {taskId} not found.");

        if (task.IsLocked)
        {
            throw new PlanningValidationException("Locked tasks cannot be reassigned.");
        }

        assignment.DeveloperId = newDeveloperId;
        await RecomputeMetricsAsync(plan, ct);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    /// <summary>Rebuilds the sprint's planning problem and recomputes a plan's metrics in place.</summary>
    private async Task RecomputeMetricsAsync(SprintPlan plan, CancellationToken ct)
    {
        var sprint = await _db.Sprints.FirstAsync(s => s.Id == plan.SprintId, ct);
        var settings = await GetSettingsAsync(ct);

        var developers = await _db.Developers
            .Include(d => d.Competencies)
            .Include(d => d.CalendarOverrides)
            .ToListAsync(ct);

        var taskIds = plan.Assignments.Select(a => a.TaskId).ToHashSet();
        var tasks = await _db.BacklogItems
            .Include(b => b.RequiredCompetencies)
            .Where(b => taskIds.Contains(b.Id))
            .ToListAsync(ct);

        var problem = _builder.Build(new PlanningProblemRequest
        {
            SprintStart = sprint.StartDate,
            SprintEnd = sprint.EndDate,
            Developers = developers,
            Tasks = tasks,
            Settings = settings,
            Weights = plan.Weights,
            RiskAppetite = 0.5m,
            History = await LoadHistoryAsync(ct),
            SprintChronology = await LoadChronologyAsync(ct)
        });

        var assignments = plan.Assignments.ToDictionary(a => a.TaskId, a => a.DeveloperId);
        plan.Metrics = _metrics.Compute(problem, assignments);

        // Refresh per-assignment derived fields.
        var devById = problem.Developers.ToDictionary(d => d.DeveloperId);
        var taskById = problem.Tasks.ToDictionary(t => t.TaskId);
        foreach (var a in plan.Assignments)
        {
            if (devById.TryGetValue(a.DeveloperId, out var dev) && taskById.TryGetValue(a.TaskId, out var t))
            {
                a.AdjustedHours = problem.AdjustedHours(dev, t);
                a.SkillFitScore = problem.SkillFit(dev, t);
                a.ContinuityBonus = t.PreferredDeveloperId == a.DeveloperId;
            }
        }
    }

    private async Task ReplaceGeneratedPlansAsync(Guid sprintId, IReadOnlyList<SprintPlan> plans, CancellationToken ct)
    {
        var existing = await _db.SprintPlans
            .Where(p => p.SprintId == sprintId && !p.IsSelected)
            .ToListAsync(ct);
        _db.SprintPlans.RemoveRange(existing);

        foreach (var plan in plans)
        {
            plan.SprintId = sprintId;
            _db.SprintPlans.Add(plan);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateAcyclic(IReadOnlyList<BacklogItem> tasks)
    {
        var ids = tasks.Select(t => t.Id);
        var edges = tasks.SelectMany(t => t.DependencyIds.Select(dep => (t.Id, dep)));
        var graph = new DependencyGraph(ids, edges);
        if (!graph.IsAcyclic())
        {
            throw new PlanningValidationException("Selected tasks contain a cyclic dependency.");
        }
    }

    private async Task<GlobalSettings> GetSettingsAsync(CancellationToken ct)
        => await _db.GlobalSettings.FirstOrDefaultAsync(ct) ?? new GlobalSettings();

    private async Task<IReadOnlyList<CompletedTaskRecord>> LoadHistoryAsync(CancellationToken ct)
    {
        // Join history with the backlog item to recover task type for β/σ-by-type.
        var query =
            from h in _db.SprintHistory
            join b in _db.BacklogItems on h.TaskId equals b.Id into bj
            from b in bj.DefaultIfEmpty()
            select new { h, Type = b != null ? b.Type : BacklogItemType.Feature };

        var rows = await query.ToListAsync(ct);
        return rows
            .Select(r => new CompletedTaskRecord(
                r.h.SprintId, r.h.DeveloperId, r.Type, r.h.EstimatedHours, r.h.ActualHours, r.h.WasCompleted))
            .ToList();
    }

    private async Task<IReadOnlyList<Guid>> LoadChronologyAsync(CancellationToken ct)
        => await _db.Sprints
            .Where(s => s.Status == SprintStatus.Completed)
            .OrderBy(s => s.StartDate)
            .Select(s => s.Id)
            .ToListAsync(ct);
}

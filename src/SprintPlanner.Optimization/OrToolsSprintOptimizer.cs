using Google.OrTools.LinearSolver;
using SprintPlanner.Application.Abstractions;
using SprintPlanner.Application.Planning;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Services;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Optimization;

/// <summary>
/// MIP-based sprint optimizer built on Google OR-Tools (SCIP backend). Solves a
/// weighted-sum assignment model under capacity, skill, dependency and locked-developer
/// constraints, then sweeps several weight profiles to produce a Pareto front (F3.2/F3.3).
/// </summary>
public sealed class OrToolsSprintOptimizer : ISprintOptimizer
{
    // Penalty per hour of capacity overflow. Large enough that the solver only overflows
    // when forced by a locked assignment, never to gain objective value.
    private const double OverflowPenalty = 100.0;

    private readonly PlanMetricsCalculator _metrics = new();

    public IReadOnlyList<SprintPlan> Generate(PlanningProblem problem)
    {
        if (problem.Tasks.Count == 0 || problem.Developers.Count == 0)
        {
            return Array.Empty<SprintPlan>();
        }

        // Corner profiles + the TL's own weights describe distinct Pareto points.
        var profiles = new List<(string Label, OptimizationWeights Weights)>
        {
            ("Your weights", problem.Weights),
            ("Maximum value", new OptimizationWeights(1m, 0m, 0m, 0m, 0m)),
            ("Minimum risk", new OptimizationWeights(0m, 0.1m, 0m, 1m, 0m)),
            ("Balanced load", new OptimizationWeights(0m, 1m, 0m, 0m, 0m)),
            ("Best skill fit", new OptimizationWeights(0.1m, 0m, 1m, 0m, 0m))
        };

        var plans = new List<SprintPlan>();
        var seenSignatures = new HashSet<string>();

        foreach (var (label, weights) in profiles)
        {
            var plan = SolveSingle(problem, weights, label);
            if (plan is null)
            {
                continue;
            }

            // Deduplicate variants with identical assignments.
            var signature = Signature(plan);
            if (seenSignatures.Add(signature))
            {
                plans.Add(plan);
            }
        }

        return plans;
    }

    private SprintPlan? SolveSingle(PlanningProblem problem, OptimizationWeights w, string label)
    {
        var solver = Solver.CreateSolver("SCIP");
        if (solver is null)
        {
            throw new InvalidOperationException("OR-Tools SCIP backend is unavailable.");
        }

        solver.SetTimeLimit((long)(problem.SolverTimeLimitSeconds * 1000));

        // Normalization denominators keep the objective terms on a comparable [0,1] scale.
        var totalBv = Math.Max(0.0001, (double)problem.Tasks.Sum(t => t.BusinessValue));
        var totalSigma = Math.Max(0.0001, (double)problem.Tasks.Sum(t => t.Sigma));
        var taskCount = problem.Tasks.Count;

        // Decision variables x[(task,dev)] for valid pairs.
        var x = new Dictionary<(Guid Task, Guid Dev), Variable>();
        foreach (var task in problem.Tasks)
        {
            foreach (var dev in problem.Developers)
            {
                var isLockedHere = task.LockedDeveloperId == dev.DeveloperId;

                // Locked task: only its locked developer gets a variable (even if not skill-eligible).
                if (task.LockedDeveloperId.HasValue && !isLockedHere)
                {
                    continue;
                }

                // Skill is a hard constraint for non-locked assignments.
                if (!isLockedHere && !problem.IsEligible(dev, task))
                {
                    continue;
                }

                x[(task.TaskId, dev.DeveloperId)] = solver.MakeBoolVar($"x_{task.TaskId}_{dev.DeveloperId}");
            }
        }

        var inclusion = BuildInclusionExpressions(problem, x);

        // Each task assigned to at most one developer; locked tasks forced on.
        foreach (var task in problem.Tasks)
        {
            if (!inclusion.TryGetValue(task.TaskId, out var incl))
            {
                continue;
            }

            if (task.LockedDeveloperId is { } lockedDev
                && x.TryGetValue((task.TaskId, lockedDev), out var lockedVar))
            {
                solver.Add(lockedVar == 1);
            }
            else
            {
                solver.Add(incl <= 1);
            }
        }

        // Dependency: a task may be included only if all its in-scope dependencies are.
        foreach (var task in problem.Tasks)
        {
            if (!inclusion.TryGetValue(task.TaskId, out var incl))
            {
                continue;
            }

            foreach (var depId in task.DependencyIds)
            {
                if (inclusion.TryGetValue(depId, out var depIncl))
                {
                    solver.Add(incl <= depIncl);
                }
                else
                {
                    // Dependency cannot be scheduled at all ⇒ this task cannot be included.
                    solver.Add(incl <= 0);
                }
            }
        }

        // Capacity (robust) with overflow slack, and the load-balance max-utilization variable.
        var overflowVars = new List<Variable>();
        var lmax = solver.MakeNumVar(0.0, double.PositiveInfinity, "Lmax");

        foreach (var dev in problem.Developers)
        {
            var adjustedLoad = new LinearExpr();
            var robustLoad = new LinearExpr();
            var hasTerms = false;

            foreach (var task in problem.Tasks)
            {
                if (!x.TryGetValue((task.TaskId, dev.DeveloperId), out var var))
                {
                    continue;
                }

                hasTerms = true;
                var adjusted = (double)problem.AdjustedHours(dev, task);
                adjustedLoad += adjusted * var;
                robustLoad += (adjusted + (double)problem.Gamma * (double)task.Sigma) * var;
            }

            if (!hasTerms)
            {
                continue;
            }

            var capacity = (double)dev.EffectiveCapacity;
            var overflow = solver.MakeNumVar(0.0, double.PositiveInfinity, $"ovf_{dev.DeveloperId}");
            overflowVars.Add(overflow);
            solver.Add(robustLoad <= capacity + overflow);

            // Lmax >= utilization_i, only meaningful when the developer has capacity.
            if (capacity > 0)
            {
                solver.Add(adjustedLoad <= capacity * lmax);
            }
        }

        // Objective.
        var objective = solver.Objective();
        foreach (var task in problem.Tasks)
        {
            foreach (var dev in problem.Developers)
            {
                if (!x.TryGetValue((task.TaskId, dev.DeveloperId), out var var))
                {
                    continue;
                }

                var continuity = task.PreferredDeveloperId == dev.DeveloperId ? 1.0 : 0.0;
                var coef =
                    (double)w.BusinessValue * (double)task.BusinessValue / totalBv
                    + (double)w.SkillFit * (double)problem.SkillFit(dev, task) / taskCount
                    + (double)w.Continuity * continuity / taskCount
                    - (double)w.RiskMinimization * (double)task.Sigma / totalSigma;
                objective.SetCoefficient(var, coef);
            }
        }

        objective.SetCoefficient(lmax, -(double)w.LoadBalance);
        foreach (var overflow in overflowVars)
        {
            objective.SetCoefficient(overflow, -OverflowPenalty);
        }

        objective.SetMaximization();

        var status = solver.Solve();
        if (status is not (Solver.ResultStatus.OPTIMAL or Solver.ResultStatus.FEASIBLE))
        {
            return null;
        }

        var assignments = x
            .Where(kv => kv.Value.SolutionValue() > 0.5)
            .ToDictionary(kv => kv.Key.Task, kv => kv.Key.Dev);

        return BuildPlan(problem, assignments, w, label);
    }

    private static Dictionary<Guid, LinearExpr> BuildInclusionExpressions(
        PlanningProblem problem,
        IReadOnlyDictionary<(Guid Task, Guid Dev), Variable> x)
    {
        var inclusion = new Dictionary<Guid, LinearExpr>();
        foreach (var task in problem.Tasks)
        {
            LinearExpr expr = new();
            var any = false;
            foreach (var dev in problem.Developers)
            {
                if (x.TryGetValue((task.TaskId, dev.DeveloperId), out var var))
                {
                    expr += var;
                    any = true;
                }
            }

            if (any)
            {
                inclusion[task.TaskId] = expr;
            }
        }

        return inclusion;
    }

    private SprintPlan BuildPlan(
        PlanningProblem problem,
        IReadOnlyDictionary<Guid, Guid> assignments,
        OptimizationWeights weights,
        string label)
    {
        var devById = problem.Developers.ToDictionary(d => d.DeveloperId);
        var taskById = problem.Tasks.ToDictionary(t => t.TaskId);

        var plan = new SprintPlan
        {
            Label = label,
            Weights = weights,
            Metrics = _metrics.Compute(problem, assignments)
        };

        var criticalNodes = ComputeCriticalNodes(problem, assignments, devById, taskById);

        foreach (var (taskId, devId) in assignments)
        {
            var dev = devById[devId];
            var task = taskById[taskId];
            plan.Assignments.Add(new TaskAssignment
            {
                SprintPlanId = plan.Id,
                TaskId = taskId,
                DeveloperId = devId,
                EstimatedHours = task.EstimatedHours,
                AdjustedHours = problem.AdjustedHours(dev, task),
                ContinuityBonus = task.PreferredDeveloperId == devId,
                SkillFitScore = problem.SkillFit(dev, task),
                IsOnCriticalPath = criticalNodes.Contains(taskId)
            });

            // Warn when a locked developer lacks the required competency (F3.2).
            if (task.LockedDeveloperId == devId && !problem.IsEligible(dev, task))
            {
                plan.Warnings.Add(
                    $"Task '{task.Title}' is locked to {dev.Name}, who does not meet its skill requirements.");
            }
        }

        // Warn on any developer whose load exceeds capacity (e.g. forced by a lock).
        foreach (var dev in problem.Developers)
        {
            if (plan.Metrics.CapacityUtilization.TryGetValue(dev.DeveloperId, out var util) && util > 1m)
            {
                plan.Warnings.Add(
                    $"{dev.Name} is overloaded at {util:P0} of capacity in this plan.");
            }
        }

        return plan;
    }

    private static HashSet<Guid> ComputeCriticalNodes(
        PlanningProblem problem,
        IReadOnlyDictionary<Guid, Guid> assignments,
        IReadOnlyDictionary<Guid, PlanningDeveloper> devById,
        IReadOnlyDictionary<Guid, PlanningTask> taskById)
    {
        var assignedIds = assignments.Keys.ToHashSet();
        if (assignedIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var edges = assignedIds.SelectMany(id =>
            taskById[id].DependencyIds.Where(assignedIds.Contains).Select(dep => (id, dep)));

        var graph = new DependencyGraph(assignedIds, edges);
        var (_, nodes) = graph.LongestPathByWeight(taskId =>
            problem.AdjustedHours(devById[assignments[taskId]], taskById[taskId]));
        return nodes.ToHashSet();
    }

    private static string Signature(SprintPlan plan)
        => string.Join("|", plan.Assignments
            .Select(a => $"{a.TaskId}:{a.DeveloperId}")
            .OrderBy(s => s));
}

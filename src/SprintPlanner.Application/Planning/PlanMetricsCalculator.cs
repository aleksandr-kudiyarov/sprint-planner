using SprintPlanner.Domain.Services;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Application.Planning;

/// <summary>
/// Computes <see cref="PlanMetrics"/> for a given set of task→developer assignments
/// against a <see cref="PlanningProblem"/>. Used by the optimizer for each generated
/// variant and again after a manual drag &amp; drop edit (F4.3), so the two paths stay
/// consistent.
/// </summary>
public sealed class PlanMetricsCalculator
{
    /// <param name="assignments">taskId → developerId. Tasks absent from the map are unassigned.</param>
    public PlanMetrics Compute(PlanningProblem problem, IReadOnlyDictionary<Guid, Guid> assignments)
    {
        var devById = problem.Developers.ToDictionary(d => d.DeveloperId);
        var taskById = problem.Tasks.ToDictionary(t => t.TaskId);

        var assigned = assignments
            .Where(kv => taskById.ContainsKey(kv.Key) && devById.ContainsKey(kv.Value))
            .ToList();

        var metrics = new PlanMetrics
        {
            TotalBusinessValue = assigned.Sum(kv => taskById[kv.Key].BusinessValue)
        };

        // Per-developer aggregates: adjusted load and σ² for the risk model.
        var load = problem.Developers.ToDictionary(d => d.DeveloperId, _ => 0m);
        var varianceSum = problem.Developers.ToDictionary(d => d.DeveloperId, _ => 0m);
        foreach (var (taskId, devId) in assigned)
        {
            var dev = devById[devId];
            var task = taskById[taskId];
            load[devId] += problem.AdjustedHours(dev, task);
            varianceSum[devId] += task.Sigma * task.Sigma;
        }

        metrics.CapacityUtilization = problem.Developers.ToDictionary(
            d => d.DeveloperId,
            d => d.EffectiveCapacity > 0 ? load[d.DeveloperId] / d.EffectiveCapacity : 0m);

        metrics.LoadBalanceScore = ComputeLoadBalance(problem, load);
        metrics.AverageSkillFit = assigned.Count == 0
            ? 0m
            : assigned.Average(kv => problem.SkillFit(devById[kv.Value], taskById[kv.Key]));
        metrics.RiskScore = ComputeRisk(problem, load, varianceSum);
        metrics.ContinuityScore = ComputeContinuity(assigned, taskById);
        metrics.CriticalPathLength = ComputeCriticalPath(problem, assigned, devById, taskById);

        return metrics;
    }

    /// <summary>Probability that any developer's load exceeds capacity (F3.4).</summary>
    public decimal OverloadProbability(decimal load, decimal varianceSum, decimal capacity)
    {
        if (capacity <= 0)
        {
            return load > 0 ? 1m : 0m;
        }

        var sigma = (decimal)Math.Sqrt((double)varianceSum);
        if (sigma <= 0)
        {
            return load > capacity ? 1m : 0m;
        }

        var z = (double)((capacity - load) / sigma);
        return (decimal)(1.0 - NormalCdf(z));
    }

    private decimal ComputeRisk(
        PlanningProblem problem,
        IReadOnlyDictionary<Guid, decimal> load,
        IReadOnlyDictionary<Guid, decimal> varianceSum)
    {
        // P(at least one overloaded) = 1 − ∏(1 − p_i).
        var noneOverloaded = 1.0m;
        foreach (var dev in problem.Developers)
        {
            var p = OverloadProbability(load[dev.DeveloperId], varianceSum[dev.DeveloperId], dev.EffectiveCapacity);
            noneOverloaded *= (1m - p);
        }

        return 1m - noneOverloaded;
    }

    private static decimal ComputeLoadBalance(PlanningProblem problem, IReadOnlyDictionary<Guid, decimal> load)
    {
        // Use utilization (load/capacity) across developers that have capacity.
        var utils = problem.Developers
            .Where(d => d.EffectiveCapacity > 0)
            .Select(d => load[d.DeveloperId] / d.EffectiveCapacity)
            .ToList();

        if (utils.Count < 2)
        {
            return 1m;
        }

        var mean = utils.Average();
        if (mean == 0)
        {
            return 1m;
        }

        var variance = utils.Sum(u => (u - mean) * (u - mean)) / utils.Count;
        var cv = (decimal)Math.Sqrt((double)variance) / mean;
        return Math.Max(0m, 1m - cv);
    }

    private static decimal ComputeContinuity(
        IReadOnlyList<KeyValuePair<Guid, Guid>> assigned,
        IReadOnlyDictionary<Guid, PlanningTask> taskById)
    {
        if (assigned.Count == 0)
        {
            return 0m;
        }

        var continuous = assigned.Count(kv =>
            taskById[kv.Key].PreferredDeveloperId is { } pref && pref == kv.Value);
        return (decimal)continuous / assigned.Count;
    }

    private static decimal ComputeCriticalPath(
        PlanningProblem problem,
        IReadOnlyList<KeyValuePair<Guid, Guid>> assigned,
        IReadOnlyDictionary<Guid, PlanningDeveloper> devById,
        IReadOnlyDictionary<Guid, PlanningTask> taskById)
    {
        var assignedIds = assigned.Select(kv => kv.Key).ToHashSet();
        if (assignedIds.Count == 0)
        {
            return 0m;
        }

        var edges = assigned.SelectMany(kv =>
            taskById[kv.Key].DependencyIds
                .Where(assignedIds.Contains)
                .Select(dep => (kv.Key, dep)));

        var graph = new DependencyGraph(assignedIds, edges);
        var assignmentMap = assigned.ToDictionary(kv => kv.Key, kv => kv.Value);

        var (length, _) = graph.LongestPathByWeight(taskId =>
            problem.AdjustedHours(devById[assignmentMap[taskId]], taskById[taskId]));
        return length;
    }

    /// <summary>Standard normal CDF via the Abramowitz &amp; Stegun erf approximation.</summary>
    private static double NormalCdf(double z) => 0.5 * (1.0 + Erf(z / Math.Sqrt(2.0)));

    private static double Erf(double x)
    {
        var sign = Math.Sign(x);
        x = Math.Abs(x);

        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
        return sign * y;
    }
}

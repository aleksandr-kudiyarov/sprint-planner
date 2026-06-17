using SprintPlanner.Application.Configuration;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Application.Statistics;

/// <summary>
/// Pure computation of developer/team statistics from completed-task records (F2).
/// Stateless and deterministic so the optimization inputs are reproducible.
/// </summary>
public sealed class StatisticsCalculator
{
    private readonly GlobalSettings _settings;

    public StatisticsCalculator(GlobalSettings settings) => _settings = settings;

    /// <summary>
    /// Computes statistics for one developer. <paramref name="sprintChronology"/> lists
    /// every completed sprint id ordered oldest → newest; only the most recent
    /// <c>VelocityWindowSprints</c> are considered for velocity/β.
    /// </summary>
    public DeveloperStatistics ComputeForDeveloper(
        Guid developerId,
        IReadOnlyList<CompletedTaskRecord> history,
        IReadOnlyList<Guid> sprintChronology)
    {
        var devHistory = history.Where(h => h.DeveloperId == developerId).ToList();

        // Restrict to the rolling window of the developer's most recent sprints.
        var windowSprints = sprintChronology
            .Where(s => devHistory.Any(h => h.SprintId == s))
            .TakeLast(_settings.VelocityWindowSprints)
            .ToList();
        var windowSet = windowSprints.ToHashSet();

        var windowed = devHistory.Where(h => windowSet.Contains(h.SprintId)).ToList();
        var completedSprintCount = windowSprints.Count;

        if (completedSprintCount == 0)
        {
            // Cold start (ADR Q4): no history at all.
            return new DeveloperStatistics
            {
                DeveloperId = developerId,
                CompletedSprintCount = 0,
                HasSufficientData = false,
                Velocity = null,
                VelocityStdDev = null,
                Trend = VelocityTrend.Unknown,
                Beta = _settings.ColdStartBeta
            };
        }

        // Per-sprint velocity = sum of actual hours of completed work, ordered chronologically.
        var perSprintVelocity = windowSprints
            .Select(s => windowed.Where(h => h.SprintId == s && h.WasCompleted).Sum(h => h.ActualHours))
            .ToList();

        var velocity = perSprintVelocity.Average();
        var velocityStdDev = StandardDeviation(perSprintVelocity);
        var trend = ComputeTrend(perSprintVelocity);

        var beta = ComputeBeta(windowed);
        var betaByType = windowed
            .GroupBy(h => h.TaskType)
            .Where(g => g.Any(h => h.EstimatedHours > 0))
            .ToDictionary(g => g.Key, g => ComputeBeta(g.ToList()));

        return new DeveloperStatistics
        {
            DeveloperId = developerId,
            CompletedSprintCount = completedSprintCount,
            HasSufficientData = completedSprintCount >= _settings.MinSprintsForReliableStats,
            Velocity = velocity,
            VelocityStdDev = velocityStdDev,
            Trend = trend,
            Beta = beta,
            BetaByType = betaByType
        };
    }

    /// <summary>
    /// β = mean(actual / estimate) over records with a positive estimate. Returns the
    /// cold-start default when no usable records exist.
    /// </summary>
    private decimal ComputeBeta(IReadOnlyCollection<CompletedTaskRecord> records)
    {
        var ratios = records
            .Where(h => h.EstimatedHours > 0 && h.WasCompleted)
            .Select(h => h.ActualHours / h.EstimatedHours)
            .ToList();

        return ratios.Count == 0 ? _settings.ColdStartBeta : ratios.Average();
    }

    /// <summary>
    /// σ for a task type from history: standard deviation of actual hours of completed
    /// tasks of that type. Falls back to <c>ColdStartSigmaFraction × estimate</c> when
    /// fewer than two samples exist (F3.4).
    /// </summary>
    public decimal ComputeSigmaForType(
        IReadOnlyList<CompletedTaskRecord> history,
        BacklogItemType type,
        decimal estimatedHours)
    {
        var actuals = history
            .Where(h => h.TaskType == type && h.WasCompleted && h.ActualHours > 0)
            .Select(h => h.ActualHours)
            .ToList();

        if (actuals.Count < 2)
        {
            return _settings.ColdStartSigmaFraction * estimatedHours;
        }

        return StandardDeviation(actuals);
    }

    private static decimal StandardDeviation(IReadOnlyCollection<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0m;
        }

        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    /// <summary>
    /// Trend from the sign of the least-squares slope over the per-sprint series.
    /// Stable when the slope magnitude is within 5% of the mean velocity per sprint.
    /// </summary>
    private static VelocityTrend ComputeTrend(IReadOnlyList<decimal> series)
    {
        if (series.Count < 2)
        {
            return VelocityTrend.Unknown;
        }

        var n = series.Count;
        var xs = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        var ys = series.Select(v => (double)v).ToList();
        var meanX = xs.Average();
        var meanY = ys.Average();

        var numerator = xs.Zip(ys, (x, y) => (x - meanX) * (y - meanY)).Sum();
        var denominator = xs.Sum(x => (x - meanX) * (x - meanX));
        if (denominator == 0)
        {
            return VelocityTrend.Stable;
        }

        var slope = numerator / denominator;
        var threshold = 0.05 * Math.Max(1.0, meanY);

        if (slope > threshold)
        {
            return VelocityTrend.Rising;
        }

        return slope < -threshold ? VelocityTrend.Falling : VelocityTrend.Stable;
    }
}

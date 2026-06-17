using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Application.Statistics;

/// <summary>
/// Aggregated statistics for one developer (F2). When <see cref="HasSufficientData"/>
/// is false the values are cold-start defaults and the UI shows the
/// "insufficient data" badge.
/// </summary>
public sealed class DeveloperStatistics
{
    public required Guid DeveloperId { get; init; }

    /// <summary>Number of completed sprints the developer participated in.</summary>
    public int CompletedSprintCount { get; init; }

    public bool HasSufficientData { get; init; }

    /// <summary>Average closed hours per sprint over the velocity window; null on cold start.</summary>
    public decimal? Velocity { get; init; }

    public decimal? VelocityStdDev { get; init; }

    public VelocityTrend Trend { get; init; } = VelocityTrend.Unknown;

    /// <summary>β = mean(actual / estimate). 1.0 on cold start (no correction).</summary>
    public decimal Beta { get; init; } = 1.0m;

    /// <summary>β broken down by task type (F2.2). Missing types fall back to <see cref="Beta"/>.</summary>
    public IReadOnlyDictionary<BacklogItemType, decimal> BetaByType { get; init; }
        = new Dictionary<BacklogItemType, decimal>();

    /// <summary>Returns the β to apply for a given task type, falling back to the overall β.</summary>
    public decimal BetaFor(BacklogItemType type)
        => BetaByType.TryGetValue(type, out var beta) ? beta : Beta;
}

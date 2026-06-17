namespace SprintPlanner.Domain.ValueObjects;

/// <summary>
/// Computed quality metrics for a single sprint plan variant (domain model §3.1).
/// </summary>
public sealed class PlanMetrics
{
    public decimal TotalBusinessValue { get; set; }

    /// <summary>Per-developer capacity utilization as a fraction (0.0 .. 1.0+).</summary>
    public Dictionary<Guid, decimal> CapacityUtilization { get; set; } = new();

    /// <summary>1 − coefficient of variation of the developers' load. Higher is more balanced.</summary>
    public decimal LoadBalanceScore { get; set; }

    public decimal AverageSkillFit { get; set; }

    /// <summary>Probability that at least one developer is overloaded (0.0 .. 1.0).</summary>
    public decimal RiskScore { get; set; }

    /// <summary>Fraction of assigned tasks that went to their preferred developer.</summary>
    public decimal ContinuityScore { get; set; }

    /// <summary>Length of the dependency critical path, in hours.</summary>
    public decimal CriticalPathLength { get; set; }
}

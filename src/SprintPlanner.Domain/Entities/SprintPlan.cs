using SprintPlanner.Domain.Common;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// One generated variant on the Pareto front (F3.3), with its assignments and metrics.
/// </summary>
public class SprintPlan : Entity
{
    public Guid SprintId { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool IsSelected { get; set; }

    /// <summary>Human-readable label for the variant, e.g. "Maximum value", "Minimum risk".</summary>
    public string Label { get; set; } = string.Empty;

    public OptimizationWeights Weights { get; set; } = OptimizationWeights.Balance;

    public List<TaskAssignment> Assignments { get; set; } = new();

    public PlanMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Non-fatal warnings produced during generation (e.g. a locked developer over
    /// capacity — F3.2). Surfaced to the TL but do not invalidate the plan.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

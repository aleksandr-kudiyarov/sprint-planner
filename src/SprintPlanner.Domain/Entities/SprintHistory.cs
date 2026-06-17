using SprintPlanner.Domain.Common;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// Actuals captured after a sprint completes (F5). Feeds velocity, β and σ statistics.
/// </summary>
public class SprintHistory : Entity
{
    public Guid SprintId { get; set; }
    public Guid TaskId { get; set; }
    public Guid DeveloperId { get; set; }

    public decimal EstimatedHours { get; set; }
    public decimal ActualHours { get; set; }

    public DateTime? CompletedAt { get; set; }
    public bool WasCompleted { get; set; }
    public string Notes { get; set; } = string.Empty;
}

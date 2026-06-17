using SprintPlanner.Domain.Common;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A planning period. Holds the generated plan variants; the selected one is the
/// committed plan (F4.4).
/// </summary>
public class Sprint : Entity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Planning;
    public string Goal { get; set; } = string.Empty;

    /// <summary>The committed plan variant, once the TL approves one.</summary>
    public Guid? SelectedPlanId { get; set; }

    public List<SprintPlan> Plans { get; set; } = new();
}

using SprintPlanner.Domain.Common;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// An exception to a developer's nominal daily capacity on a specific date
/// (vacation, off-site, reduced day). <see cref="AvailableHours"/> of 0 means
/// the developer is fully unavailable that day.
/// </summary>
public class CalendarOverride : Entity
{
    public Guid DeveloperId { get; set; }
    public DateOnly Date { get; set; }
    public decimal AvailableHours { get; set; }
}

using SprintPlanner.Domain.Common;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A team member who can be assigned backlog items.
/// </summary>
public class Developer : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>Nominal hours per working day available for tasks (e.g. 6.0).</summary>
    public decimal CapacityHoursPerDay { get; set; }

    public List<CalendarOverride> CalendarOverrides { get; set; } = new();
    public List<DeveloperCompetency> Competencies { get; set; } = new();

    /// <summary>
    /// Effective capacity over a sprint window (F1.1): the sum of available hours for
    /// each working day in [start, end]. Weekends contribute nothing by default; a
    /// <see cref="CalendarOverride"/> replaces the nominal value for its date (and can
    /// also grant hours on a weekend if explicitly set).
    /// </summary>
    public decimal EffectiveCapacity(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("Sprint end date must not precede start date.");
        }

        var overrides = CalendarOverrides
            .GroupBy(o => o.Date)
            .ToDictionary(g => g.Key, g => g.Last().AvailableHours);

        decimal total = 0m;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (overrides.TryGetValue(day, out var hours))
            {
                total += Math.Max(0m, hours);
            }
            else if (IsWorkingDay(day))
            {
                total += CapacityHoursPerDay;
            }
        }

        return total;
    }

    /// <summary>
    /// Cold-start velocity fallback (F2.1) when no completed-sprint history exists:
    /// <c>capacityHoursPerDay × working days × effectiveCoefficient</c>.
    /// </summary>
    public decimal ColdStartVelocity(DateOnly start, DateOnly end, decimal effectiveCoefficient)
    {
        var workingDays = CountWorkingDays(start, end);
        return CapacityHoursPerDay * workingDays * effectiveCoefficient;
    }

    public static bool IsWorkingDay(DateOnly date)
        => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static int CountWorkingDays(DateOnly start, DateOnly end)
    {
        var count = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
            {
                count++;
            }
        }

        return count;
    }
}

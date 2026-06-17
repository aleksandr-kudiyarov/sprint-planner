using FluentAssertions;
using SprintPlanner.Domain.Entities;

namespace SprintPlanner.Domain.Tests;

public class DeveloperCapacityTests
{
    private static Developer Dev(decimal perDay = 6m) => new()
    {
        Name = "Test",
        CapacityHoursPerDay = perDay
    };

    [Fact]
    public void EffectiveCapacity_SumsWorkingDays_ExcludingWeekends()
    {
        var dev = Dev(6m);
        // Mon 2026-06-15 .. Fri 2026-06-19 = 5 working days.
        var start = new DateOnly(2026, 6, 15);
        var end = new DateOnly(2026, 6, 19);

        dev.EffectiveCapacity(start, end).Should().Be(30m);
    }

    [Fact]
    public void EffectiveCapacity_SkipsWeekends_OverFullWeek()
    {
        var dev = Dev(6m);
        // Mon .. Sun = 5 working days, weekend excluded.
        var start = new DateOnly(2026, 6, 15);
        var end = new DateOnly(2026, 6, 21);

        dev.EffectiveCapacity(start, end).Should().Be(30m);
    }

    [Fact]
    public void EffectiveCapacity_AppliesCalendarOverride_ForVacationDay()
    {
        var dev = Dev(6m);
        var start = new DateOnly(2026, 6, 15);
        var end = new DateOnly(2026, 6, 19);
        // Wednesday off.
        dev.CalendarOverrides.Add(new CalendarOverride
        {
            Date = new DateOnly(2026, 6, 17),
            AvailableHours = 0m
        });

        dev.EffectiveCapacity(start, end).Should().Be(24m);
    }

    [Fact]
    public void EffectiveCapacity_OverrideCanGrantHoursOnWeekend()
    {
        var dev = Dev(6m);
        var saturday = new DateOnly(2026, 6, 20);
        dev.CalendarOverrides.Add(new CalendarOverride { Date = saturday, AvailableHours = 4m });

        dev.EffectiveCapacity(saturday, saturday).Should().Be(4m);
    }

    [Fact]
    public void EffectiveCapacity_Throws_WhenEndBeforeStart()
    {
        var dev = Dev();
        var act = () => dev.EffectiveCapacity(new DateOnly(2026, 6, 19), new DateOnly(2026, 6, 15));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ColdStartVelocity_UsesEffectiveCoefficient()
    {
        var dev = Dev(6m);
        // 5 working days × 6h × 0.8 = 24.
        var start = new DateOnly(2026, 6, 15);
        var end = new DateOnly(2026, 6, 19);

        dev.ColdStartVelocity(start, end, 0.8m).Should().Be(24m);
    }
}

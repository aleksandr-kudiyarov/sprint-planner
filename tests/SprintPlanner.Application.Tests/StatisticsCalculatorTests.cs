using FluentAssertions;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Application.Statistics;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Application.Tests;

public class StatisticsCalculatorTests
{
    private readonly GlobalSettings _settings = new();
    private readonly StatisticsCalculator _calc;

    public StatisticsCalculatorTests() => _calc = new StatisticsCalculator(_settings);

    [Fact]
    public void ColdStart_NoHistory_ReturnsDefaults()
    {
        var dev = Guid.NewGuid();
        var stats = _calc.ComputeForDeveloper(dev, Array.Empty<CompletedTaskRecord>(), Array.Empty<Guid>());

        stats.HasSufficientData.Should().BeFalse();
        stats.Velocity.Should().BeNull();
        stats.Beta.Should().Be(1.0m);
        stats.Trend.Should().Be(VelocityTrend.Unknown);
    }

    [Fact]
    public void Beta_IsMeanOfActualOverEstimate()
    {
        var dev = Guid.NewGuid();
        var sprint = Guid.NewGuid();
        var history = new[]
        {
            new CompletedTaskRecord(sprint, dev, BacklogItemType.Feature, 10m, 20m, true), // ratio 2.0
            new CompletedTaskRecord(sprint, dev, BacklogItemType.Feature, 10m, 10m, true)  // ratio 1.0
        };

        var stats = _calc.ComputeForDeveloper(dev, history, new[] { sprint });

        stats.Beta.Should().Be(1.5m);
    }

    [Fact]
    public void BetaByType_SplitsByTaskType()
    {
        var dev = Guid.NewGuid();
        var sprint = Guid.NewGuid();
        var history = new[]
        {
            new CompletedTaskRecord(sprint, dev, BacklogItemType.Bug, 10m, 10m, true),     // bug ratio 1.0
            new CompletedTaskRecord(sprint, dev, BacklogItemType.Feature, 10m, 30m, true)  // feature ratio 3.0
        };

        var stats = _calc.ComputeForDeveloper(dev, history, new[] { sprint });

        stats.BetaFor(BacklogItemType.Bug).Should().Be(1.0m);
        stats.BetaFor(BacklogItemType.Feature).Should().Be(3.0m);
        // Unknown type falls back to overall β.
        stats.BetaFor(BacklogItemType.Chore).Should().Be(stats.Beta);
    }

    [Fact]
    public void Velocity_AveragesCompletedHoursPerSprint()
    {
        var dev = Guid.NewGuid();
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var history = new[]
        {
            new CompletedTaskRecord(s1, dev, BacklogItemType.Feature, 10m, 20m, true),
            new CompletedTaskRecord(s2, dev, BacklogItemType.Feature, 10m, 40m, true)
        };

        var stats = _calc.ComputeForDeveloper(dev, history, new[] { s1, s2 });

        stats.Velocity.Should().Be(30m); // (20 + 40) / 2
        stats.CompletedSprintCount.Should().Be(2);
    }

    [Fact]
    public void Velocity_RisingTrend_Detected()
    {
        var dev = Guid.NewGuid();
        var sprints = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var hours = new decimal[] { 10m, 20m, 30m, 40m };
        var history = sprints
            .Select((s, i) => new CompletedTaskRecord(s, dev, BacklogItemType.Feature, 10m, hours[i], true))
            .ToArray();

        var stats = _calc.ComputeForDeveloper(dev, history, sprints);

        stats.Trend.Should().Be(VelocityTrend.Rising);
    }

    [Fact]
    public void Sigma_FallsBackToColdStartFraction_WithFewSamples()
    {
        var sigma = _calc.ComputeSigmaForType(Array.Empty<CompletedTaskRecord>(), BacklogItemType.Feature, 10m);
        sigma.Should().Be(3.0m); // 0.3 × 10
    }
}

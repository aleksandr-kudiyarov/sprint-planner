using FluentAssertions;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Domain.Tests;

public class OptimizationWeightsTests
{
    [Fact]
    public void Constructor_NormalizesToSumOfOne()
    {
        var w = new OptimizationWeights(2m, 2m, 2m, 2m, 2m);

        (w.BusinessValue + w.LoadBalance + w.SkillFit + w.RiskMinimization + w.Continuity)
            .Should().Be(1.0m);
        w.BusinessValue.Should().Be(0.2m);
    }

    [Fact]
    public void PresetProfiles_AreNormalized()
    {
        foreach (var w in new[] { OptimizationWeights.Value, OptimizationWeights.Stability, OptimizationWeights.Balance })
        {
            (w.BusinessValue + w.LoadBalance + w.SkillFit + w.RiskMinimization + w.Continuity)
                .Should().BeApproximately(1.0m, 0.0001m);
        }
    }

    [Fact]
    public void ValueProfile_FavoursBusinessValue()
    {
        OptimizationWeights.Value.BusinessValue.Should().Be(0.5m);
    }

    [Fact]
    public void Constructor_Throws_OnNegativeWeight()
    {
        var act = () => new OptimizationWeights(-1m, 1m, 1m, 1m, 1m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Throws_WhenAllZero()
    {
        var act = () => new OptimizationWeights(0m, 0m, 0m, 0m, 0m);
        act.Should().Throw<ArgumentException>();
    }
}

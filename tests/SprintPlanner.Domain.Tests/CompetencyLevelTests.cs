using FluentAssertions;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Domain.Tests;

public class CompetencyLevelTests
{
    [Theory]
    [InlineData(CompetencyLevel.None, 0.0)]
    [InlineData(CompetencyLevel.Junior, 0.25)]
    [InlineData(CompetencyLevel.Mid, 0.5)]
    [InlineData(CompetencyLevel.Senior, 0.75)]
    [InlineData(CompetencyLevel.Expert, 1.0)]
    public void ToNumeric_MapsLevelsToWeights(CompetencyLevel level, double expected)
    {
        level.ToNumeric().Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData(CompetencyLevel.Senior, CompetencyLevel.Mid, true)]
    [InlineData(CompetencyLevel.Mid, CompetencyLevel.Mid, true)]
    [InlineData(CompetencyLevel.Junior, CompetencyLevel.Senior, false)]
    [InlineData(CompetencyLevel.None, CompetencyLevel.Junior, false)]
    public void Satisfies_ComparesOrdinally(CompetencyLevel owned, CompetencyLevel required, bool expected)
    {
        owned.Satisfies(required).Should().Be(expected);
    }
}

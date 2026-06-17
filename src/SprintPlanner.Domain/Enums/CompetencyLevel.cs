namespace SprintPlanner.Domain.Enums;

/// <summary>
/// Ordinal proficiency of a developer in a competency. The numeric ordering is
/// meaningful: a requirement of <see cref="Mid"/> is satisfied by any level &gt;= Mid.
/// </summary>
public enum CompetencyLevel
{
    None = 0,
    Junior = 1,
    Mid = 2,
    Senior = 3,
    Expert = 4
}

public static class CompetencyLevelExtensions
{
    /// <summary>
    /// Default mapping from the ordinal level to a normalized [0.0 .. 1.0] weight used
    /// in skill-fit calculations. The TL may override the stored <c>LevelNumeric</c>,
    /// but this provides a sensible default when a level is (re)assigned.
    /// </summary>
    public static decimal ToNumeric(this CompetencyLevel level) => level switch
    {
        CompetencyLevel.None => 0.0m,
        CompetencyLevel.Junior => 0.25m,
        CompetencyLevel.Mid => 0.50m,
        CompetencyLevel.Senior => 0.75m,
        CompetencyLevel.Expert => 1.0m,
        _ => 0.0m
    };

    /// <summary>True when this level satisfies the given minimum requirement.</summary>
    public static bool Satisfies(this CompetencyLevel level, CompetencyLevel minimumRequired)
        => level >= minimumRequired;
}

using SprintPlanner.Domain.Common;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A developer's ownership of a competency at a given level. The level history is
/// kept separately (see <see cref="DeveloperCompetencyHistory"/>) so that the TL can
/// audit how proficiency assessments changed over time.
/// </summary>
public class DeveloperCompetency : Entity
{
    public Guid DeveloperId { get; set; }
    public Guid CompetencyId { get; set; }
    public CompetencyLevel Level { get; set; } = CompetencyLevel.None;

    /// <summary>Normalized [0.0 .. 1.0] weight used in skill-fit calculations.</summary>
    public decimal LevelNumeric { get; set; }

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;

    public Competency? Competency { get; set; }

    /// <summary>
    /// Applies a new level, refreshing the numeric weight to the level's default and
    /// stamping audit fields. Returns the previous level for history bookkeeping.
    /// </summary>
    public CompetencyLevel ApplyLevel(CompetencyLevel newLevel, string updatedBy, decimal? numericOverride = null)
    {
        var previous = Level;
        Level = newLevel;
        LevelNumeric = numericOverride ?? newLevel.ToNumeric();
        UpdatedBy = updatedBy;
        LastUpdated = DateTime.UtcNow;
        return previous;
    }
}

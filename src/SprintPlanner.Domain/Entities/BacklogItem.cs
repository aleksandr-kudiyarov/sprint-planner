using SprintPlanner.Domain.Common;
using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Domain.Entities;

/// <summary>
/// A unit of work that can be planned into a sprint. Estimation unit is hours only
/// (ADR Q1 — story points removed).
/// </summary>
public class BacklogItem : Entity
{
    /// <summary>Optional free-text reference to YouTrack/Jira (ADR Q2, no sync).</summary>
    public string? ExternalId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BacklogItemType Type { get; set; } = BacklogItemType.Feature;

    /// <summary>Lower number = higher priority.</summary>
    public int Priority { get; set; }

    /// <summary>Relative business value weight.</summary>
    public decimal BusinessValue { get; set; }

    public decimal EstimatedHours { get; set; }

    public BacklogItemStatus Status { get; set; } = BacklogItemStatus.New;

    /// <summary>Preferred assignee for continuity bonus.</summary>
    public Guid? PreferredDeveloperId { get; set; }

    /// <summary>Hard pin to a developer (ADR Q5): solver forces x[locked][j] = 1.</summary>
    public Guid? LockedDeveloperId { get; set; }

    /// <summary>Who performed this work in the previous sprint (auto-remembered).</summary>
    public Guid? LastDeveloperId { get; set; }

    public List<CompetencyRequirement> RequiredCompetencies { get; set; } = new();

    /// <summary>IDs of items that must be included in the sprint before this one.</summary>
    public List<Guid> DependencyIds { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public bool IsLocked => LockedDeveloperId.HasValue;
}

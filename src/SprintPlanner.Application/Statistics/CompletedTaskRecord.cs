using SprintPlanner.Domain.Enums;

namespace SprintPlanner.Application.Statistics;

/// <summary>
/// A flattened sprint-history row enriched with the task's type. Built by the service
/// layer (joining <c>SprintHistory</c> with <c>BacklogItem</c>) and fed to
/// <see cref="StatisticsCalculator"/>, keeping the calculator free of persistence.
/// </summary>
public sealed record CompletedTaskRecord(
    Guid SprintId,
    Guid DeveloperId,
    BacklogItemType TaskType,
    decimal EstimatedHours,
    decimal ActualHours,
    bool WasCompleted);

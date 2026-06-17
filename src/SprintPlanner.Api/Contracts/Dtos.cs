using SprintPlanner.Domain.Enums;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Api.Contracts;

// ----- Developers -----

public sealed record CalendarOverrideDto(DateOnly Date, decimal AvailableHours);

public sealed record DeveloperCompetencyDto(Guid CompetencyId, CompetencyLevel Level, decimal LevelNumeric);

public sealed record DeveloperDto(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    decimal CapacityHoursPerDay,
    IReadOnlyList<CalendarOverrideDto> CalendarOverrides,
    IReadOnlyList<DeveloperCompetencyDto> Competencies);

public sealed record CreateDeveloperRequest(
    string Name, string Email, decimal CapacityHoursPerDay);

public sealed record UpdateDeveloperRequest(
    string Name, string Email, decimal CapacityHoursPerDay, bool IsActive,
    IReadOnlyList<CalendarOverrideDto>? CalendarOverrides);

public sealed record SetCompetencyLevelRequest(Guid CompetencyId, CompetencyLevel Level, decimal? LevelNumeric);

// ----- Competencies -----

public sealed record CompetencyDto(Guid Id, string Name, string Description);

public sealed record CreateCompetencyRequest(string Name, string Description);

// ----- Backlog -----

public sealed record CompetencyRequirementDto(Guid CompetencyId, CompetencyLevel MinimumLevel);

public sealed record BacklogItemDto(
    Guid Id,
    string? ExternalId,
    string Title,
    string Description,
    BacklogItemType Type,
    int Priority,
    decimal BusinessValue,
    decimal EstimatedHours,
    BacklogItemStatus Status,
    Guid? PreferredDeveloperId,
    Guid? LockedDeveloperId,
    Guid? LastDeveloperId,
    IReadOnlyList<CompetencyRequirementDto> RequiredCompetencies,
    IReadOnlyList<Guid> DependencyIds,
    IReadOnlyList<string> Tags);

public sealed record UpsertBacklogItemRequest(
    string? ExternalId,
    string Title,
    string Description,
    BacklogItemType Type,
    int Priority,
    decimal BusinessValue,
    decimal EstimatedHours,
    BacklogItemStatus Status,
    Guid? PreferredDeveloperId,
    Guid? LockedDeveloperId,
    IReadOnlyList<CompetencyRequirementDto>? RequiredCompetencies,
    IReadOnlyList<Guid>? DependencyIds,
    IReadOnlyList<string>? Tags);

// ----- Sprints -----

public sealed record SprintDto(
    Guid Id, string Name, DateOnly StartDate, DateOnly EndDate,
    SprintStatus Status, string Goal, Guid? SelectedPlanId);

public sealed record UpsertSprintRequest(
    string Name, DateOnly StartDate, DateOnly EndDate, string Goal);

// ----- Planning -----

public sealed record OptimizationWeightsDto(
    decimal BusinessValue, decimal LoadBalance, decimal SkillFit,
    decimal RiskMinimization, decimal Continuity)
{
    public OptimizationWeights ToDomain()
        => new(BusinessValue, LoadBalance, SkillFit, RiskMinimization, Continuity);

    public static OptimizationWeightsDto From(OptimizationWeights w)
        => new(w.BusinessValue, w.LoadBalance, w.SkillFit, w.RiskMinimization, w.Continuity);
}

public sealed record GeneratePlansRequest(
    IReadOnlyList<Guid> TaskIds,
    OptimizationWeightsDto Weights,
    decimal RiskAppetite);

public sealed record ReassignTaskRequest(Guid TaskId, Guid NewDeveloperId);

public sealed record TaskAssignmentDto(
    Guid TaskId, Guid DeveloperId, decimal EstimatedHours, decimal AdjustedHours,
    bool ContinuityBonus, decimal SkillFitScore, bool IsOnCriticalPath);

public sealed record PlanMetricsDto(
    decimal TotalBusinessValue,
    IReadOnlyDictionary<Guid, decimal> CapacityUtilization,
    decimal LoadBalanceScore,
    decimal AverageSkillFit,
    decimal RiskScore,
    decimal ContinuityScore,
    decimal CriticalPathLength);

public sealed record SprintPlanDto(
    Guid Id,
    Guid SprintId,
    DateTime GeneratedAt,
    bool IsSelected,
    string Label,
    OptimizationWeightsDto Weights,
    IReadOnlyList<TaskAssignmentDto> Assignments,
    PlanMetricsDto Metrics,
    IReadOnlyList<string> Warnings);

// ----- Feedback (F5) -----

public sealed record SprintFeedbackItem(Guid TaskId, Guid DeveloperId, decimal ActualHours, bool WasCompleted, string? Notes);

public sealed record SprintFeedbackRequest(IReadOnlyList<SprintFeedbackItem> Items);

// ----- Statistics (F2) -----

public sealed record DeveloperStatisticsDto(
    Guid DeveloperId,
    int CompletedSprintCount,
    bool HasSufficientData,
    decimal? Velocity,
    decimal? VelocityStdDev,
    string Trend,
    decimal Beta);

// ----- Settings (F6.2) -----

public sealed record GlobalSettingsDto(
    int DefaultSprintLengthDays,
    decimal EffectiveWorkCoefficient,
    int VelocityWindowSprints,
    decimal LoadWarningThreshold,
    int MinSprintsForReliableStats,
    decimal ColdStartBeta,
    decimal ColdStartSigmaFraction);

using SprintPlanner.Application.Statistics;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Api.Contracts;

/// <summary>Entity ↔ DTO mapping. Kept explicit (no AutoMapper) for clarity and trimming.</summary>
internal static class Mapping
{
    public static DeveloperDto ToDto(this Developer d) => new(
        d.Id, d.Name, d.Email, d.IsActive, d.CapacityHoursPerDay,
        d.CalendarOverrides.Select(o => new CalendarOverrideDto(o.Date, o.AvailableHours)).ToList(),
        d.Competencies.Select(c => new DeveloperCompetencyDto(c.CompetencyId, c.Level, c.LevelNumeric)).ToList());

    public static CompetencyDto ToDto(this Competency c) => new(c.Id, c.Name, c.Description);

    public static BacklogItemDto ToDto(this BacklogItem b) => new(
        b.Id, b.ExternalId, b.Title, b.Description, b.Type, b.Priority, b.BusinessValue,
        b.EstimatedHours, b.Status, b.PreferredDeveloperId, b.LockedDeveloperId, b.LastDeveloperId,
        b.RequiredCompetencies.Select(r => new CompetencyRequirementDto(r.CompetencyId, r.MinimumLevel)).ToList(),
        b.DependencyIds.ToList(), b.Tags.ToList());

    public static SprintDto ToDto(this Sprint s) => new(
        s.Id, s.Name, s.StartDate, s.EndDate, s.Status, s.Goal, s.SelectedPlanId);

    public static SprintPlanDto ToDto(this SprintPlan p) => new(
        p.Id, p.SprintId, p.GeneratedAt, p.IsSelected, p.Label,
        OptimizationWeightsDto.From(p.Weights),
        p.Assignments.Select(a => new TaskAssignmentDto(
            a.TaskId, a.DeveloperId, a.EstimatedHours, a.AdjustedHours,
            a.ContinuityBonus, a.SkillFitScore, a.IsOnCriticalPath)).ToList(),
        p.Metrics.ToDto(),
        p.Warnings.ToList());

    public static PlanMetricsDto ToDto(this PlanMetrics m) => new(
        m.TotalBusinessValue, m.CapacityUtilization, m.LoadBalanceScore, m.AverageSkillFit,
        m.RiskScore, m.ContinuityScore, m.CriticalPathLength);

    public static DeveloperStatisticsDto ToDto(this DeveloperStatistics s) => new(
        s.DeveloperId, s.CompletedSprintCount, s.HasSufficientData,
        s.Velocity, s.VelocityStdDev, s.Trend.ToString(), s.Beta);

    public static GlobalSettingsDto ToDto(this GlobalSettings s) => new(
        s.DefaultSprintLengthDays, s.EffectiveWorkCoefficient, s.VelocityWindowSprints,
        s.LoadWarningThreshold, s.MinSprintsForReliableStats, s.ColdStartBeta, s.ColdStartSigmaFraction);
}

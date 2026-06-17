using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Domain.Entities;

namespace SprintPlanner.Infrastructure.Persistence.Configurations;

internal sealed class DeveloperConfiguration : IEntityTypeConfiguration<Developer>
{
    public void Configure(EntityTypeBuilder<Developer> builder)
    {
        builder.ToTable("developers");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Email).HasMaxLength(320).IsRequired();
        builder.Property(d => d.CapacityHoursPerDay).HasPrecision(6, 2);

        builder.HasMany(d => d.Competencies)
            .WithOne()
            .HasForeignKey(c => c.DeveloperId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.CalendarOverrides)
            .WithOne()
            .HasForeignKey(o => o.DeveloperId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CalendarOverrideConfiguration : IEntityTypeConfiguration<CalendarOverride>
{
    public void Configure(EntityTypeBuilder<CalendarOverride> builder)
    {
        builder.ToTable("calendar_overrides");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.AvailableHours).HasPrecision(6, 2);
        builder.HasIndex(o => new { o.DeveloperId, o.Date });
    }
}

internal sealed class CompetencyConfiguration : IEntityTypeConfiguration<Competency>
{
    public void Configure(EntityTypeBuilder<Competency> builder)
    {
        builder.ToTable("competencies");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

internal sealed class DeveloperCompetencyConfiguration : IEntityTypeConfiguration<DeveloperCompetency>
{
    public void Configure(EntityTypeBuilder<DeveloperCompetency> builder)
    {
        builder.ToTable("developer_competencies");
        builder.HasKey(dc => dc.Id);
        builder.Property(dc => dc.Level).HasConversion<string>().HasMaxLength(20);
        builder.Property(dc => dc.LevelNumeric).HasPrecision(4, 3);
        builder.Property(dc => dc.UpdatedBy).HasMaxLength(200);
        builder.Ignore(dc => dc.Competency);
        builder.HasIndex(dc => new { dc.DeveloperId, dc.CompetencyId }).IsUnique();
    }
}

internal sealed class DeveloperCompetencyHistoryConfiguration : IEntityTypeConfiguration<DeveloperCompetencyHistory>
{
    public void Configure(EntityTypeBuilder<DeveloperCompetencyHistory> builder)
    {
        builder.ToTable("developer_competency_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.FromLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ToLevel).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ChangedBy).HasMaxLength(200);
        builder.HasIndex(h => new { h.DeveloperId, h.CompetencyId });
    }
}

internal sealed class BacklogItemConfiguration : IEntityTypeConfiguration<BacklogItem>
{
    public void Configure(EntityTypeBuilder<BacklogItem> builder)
    {
        builder.ToTable("backlog_items");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Title).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Description).HasMaxLength(8000);
        builder.Property(b => b.ExternalId).HasMaxLength(100);
        builder.Property(b => b.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.BusinessValue).HasPrecision(10, 2);
        builder.Property(b => b.EstimatedHours).HasPrecision(8, 2);

        builder.Property(b => b.DependencyIds).AsJsonbList();
        builder.Property(b => b.Tags).AsJsonbList();

        builder.HasMany(b => b.RequiredCompetencies)
            .WithOne()
            .HasForeignKey(r => r.BacklogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => b.ExternalId);
    }
}

internal sealed class CompetencyRequirementConfiguration : IEntityTypeConfiguration<CompetencyRequirement>
{
    public void Configure(EntityTypeBuilder<CompetencyRequirement> builder)
    {
        builder.ToTable("competency_requirements");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.MinimumLevel).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(r => r.Competency);
    }
}

internal sealed class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("sprints");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Goal).HasMaxLength(2000);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(s => s.Plans)
            .WithOne()
            .HasForeignKey(p => p.SprintId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SprintPlanConfiguration : IEntityTypeConfiguration<SprintPlan>
{
    public void Configure(EntityTypeBuilder<SprintPlan> builder)
    {
        builder.ToTable("sprint_plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Label).HasMaxLength(100);
        builder.Property(p => p.Weights).AsJsonb();
        builder.Property(p => p.Metrics).AsJsonb();
        builder.Property(p => p.Warnings).AsJsonbList();

        builder.HasMany(p => p.Assignments)
            .WithOne()
            .HasForeignKey(a => a.SprintPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        builder.ToTable("task_assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EstimatedHours).HasPrecision(8, 2);
        builder.Property(a => a.AdjustedHours).HasPrecision(8, 2);
        builder.Property(a => a.SkillFitScore).HasPrecision(4, 3);
        builder.HasIndex(a => a.SprintPlanId);
    }
}

internal sealed class SprintHistoryConfiguration : IEntityTypeConfiguration<SprintHistory>
{
    public void Configure(EntityTypeBuilder<SprintHistory> builder)
    {
        builder.ToTable("sprint_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.EstimatedHours).HasPrecision(8, 2);
        builder.Property(h => h.ActualHours).HasPrecision(8, 2);
        builder.Property(h => h.Notes).HasMaxLength(4000);
        builder.HasIndex(h => h.SprintId);
        builder.HasIndex(h => new { h.DeveloperId, h.TaskId });
    }
}

internal sealed class GlobalSettingsConfiguration : IEntityTypeConfiguration<GlobalSettings>
{
    public void Configure(EntityTypeBuilder<GlobalSettings> builder)
    {
        builder.ToTable("global_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.EffectiveWorkCoefficient).HasPrecision(4, 3);
        builder.Property(s => s.LoadWarningThreshold).HasPrecision(4, 3);
        builder.Property(s => s.ColdStartBeta).HasPrecision(4, 3);
        builder.Property(s => s.ColdStartSigmaFraction).HasPrecision(4, 3);
    }
}

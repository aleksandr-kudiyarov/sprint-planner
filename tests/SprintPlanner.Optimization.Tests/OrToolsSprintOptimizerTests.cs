using FluentAssertions;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Optimization.Tests;

public class OrToolsSprintOptimizerTests
{
    private readonly OrToolsSprintOptimizer _optimizer = new();

    [Fact]
    public void Generate_AssignsTasks_WhenCapacityIsSufficient()
    {
        var builder = new ProblemBuilder();
        builder.AddDeveloper("Ann", capacity: 40m);
        builder.AddTask("T1", estimatedHours: 10m, businessValue: 5m);
        builder.AddTask("T2", estimatedHours: 10m, businessValue: 5m);
        var problem = builder.Build();

        var plans = _optimizer.Generate(problem);

        plans.Should().NotBeEmpty();
        var best = plans.First();
        best.Assignments.Should().HaveCount(2);
    }

    [Fact]
    public void Generate_NeverViolatesCapacity_ForUnlockedTasks()
    {
        // 5 tasks × 10h = 50h but only 30h of capacity: solver must leave some out.
        var builder = new ProblemBuilder().Weights(OptimizationWeights.Value);
        builder.AddDeveloper("Ann", capacity: 30m);
        for (var i = 0; i < 5; i++)
        {
            builder.AddTask($"T{i}", estimatedHours: 10m, businessValue: 1m);
        }

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        foreach (var plan in plans)
        {
            foreach (var util in plan.Metrics.CapacityUtilization.Values)
            {
                util.Should().BeLessThanOrEqualTo(1.0m);
            }
        }
    }

    [Fact]
    public void Generate_RespectsSkillConstraint()
    {
        var kafka = Guid.NewGuid();
        var builder = new ProblemBuilder();
        // Only Bob has Expert Kafka.
        var ann = builder.AddDeveloper("Ann", 40m, skills: new[] { (kafka, CompetencyLevel.Junior) });
        var bob = builder.AddDeveloper("Bob", 40m, skills: new[] { (kafka, CompetencyLevel.Expert) });
        var task = builder.AddTask("Kafka work", 10m,
            requires: new[] { (kafka, CompetencyLevel.Expert) });

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        foreach (var plan in plans)
        {
            var assignment = plan.Assignments.SingleOrDefault(a => a.TaskId == task.TaskId);
            if (assignment is not null)
            {
                assignment.DeveloperId.Should().Be(bob.DeveloperId);
            }
        }

        // Sanity: Ann should never get it.
        plans.SelectMany(p => p.Assignments)
            .Where(a => a.TaskId == task.TaskId)
            .Should().OnlyContain(a => a.DeveloperId == bob.DeveloperId);
    }

    [Fact]
    public void Generate_DoesNotAssign_WhenNoDeveloperMeetsSkill()
    {
        var expertOnly = Guid.NewGuid();
        var builder = new ProblemBuilder();
        builder.AddDeveloper("Ann", 40m, skills: new[] { (expertOnly, CompetencyLevel.Mid) });
        var task = builder.AddTask("Hard", 10m, requires: new[] { (expertOnly, CompetencyLevel.Expert) });

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        plans.SelectMany(p => p.Assignments)
            .Should().NotContain(a => a.TaskId == task.TaskId);
    }

    [Fact]
    public void Generate_RespectsDependencies_DependentRequiresDependency()
    {
        var builder = new ProblemBuilder().Weights(OptimizationWeights.Value);
        builder.AddDeveloper("Ann", 40m);
        var a = builder.AddTask("A", 10m, businessValue: 1m);
        var b = builder.AddTask("B", 10m, businessValue: 1m, dependsOn: new[] { a.TaskId });

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        foreach (var plan in plans)
        {
            var hasA = plan.Assignments.Any(x => x.TaskId == a.TaskId);
            var hasB = plan.Assignments.Any(x => x.TaskId == b.TaskId);
            if (hasB)
            {
                hasA.Should().BeTrue("B depends on A");
            }
        }
    }

    [Fact]
    public void Generate_ExcludesDependent_WhenDependencyUnschedulable()
    {
        var expert = Guid.NewGuid();
        var builder = new ProblemBuilder().Weights(OptimizationWeights.Value);
        builder.AddDeveloper("Ann", 40m, skills: new[] { (expert, CompetencyLevel.Mid) });
        // A requires Expert that nobody has → A cannot be scheduled.
        var a = builder.AddTask("A", 10m, requires: new[] { (expert, CompetencyLevel.Expert) });
        var b = builder.AddTask("B", 10m, dependsOn: new[] { a.TaskId });

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        plans.SelectMany(p => p.Assignments)
            .Should().NotContain(x => x.TaskId == b.TaskId);
    }

    [Fact]
    public void Generate_HonoursLockedDeveloper_EvenWithoutSkill()
    {
        var kafka = Guid.NewGuid();
        var builder = new ProblemBuilder();
        // Ann is locked to the task but only Junior, while it needs Expert.
        var ann = builder.AddDeveloper("Ann", 40m, skills: new[] { (kafka, CompetencyLevel.Junior) });
        var task = builder.AddTask("Locked Kafka", 10m,
            requires: new[] { (kafka, CompetencyLevel.Expert) },
            lockedDeveloperId: ann.DeveloperId);

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        foreach (var plan in plans)
        {
            var assignment = plan.Assignments.Single(a => a.TaskId == task.TaskId);
            assignment.DeveloperId.Should().Be(ann.DeveloperId);
            plan.Warnings.Should().Contain(w => w.Contains("locked"));
        }
    }

    [Fact]
    public void Generate_LockedTask_OverCapacity_ProducesWarningNotFailure()
    {
        var builder = new ProblemBuilder();
        var ann = builder.AddDeveloper("Ann", capacity: 5m);
        // 20h locked task into a 5h-capacity developer.
        builder.AddTask("Big locked", 20m, lockedDeveloperId: ann.DeveloperId);

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        plans.Should().NotBeEmpty();
        plans.First().Assignments.Should().ContainSingle();
        plans.First().Warnings.Should().Contain(w => w.Contains("overloaded"));
    }

    [Fact]
    public void Generate_ProducesDistinctParetoVariants()
    {
        var builder = new ProblemBuilder();
        builder.AddDeveloper("Ann", 30m);
        builder.AddDeveloper("Bob", 30m);
        for (var i = 0; i < 6; i++)
        {
            builder.AddTask($"T{i}", estimatedHours: 8m, businessValue: i + 1, sigma: 2m);
        }

        var problem = builder.Build();
        var plans = _optimizer.Generate(problem);

        plans.Should().HaveCountGreaterThanOrEqualTo(1);
        plans.Should().HaveCountLessThanOrEqualTo(5);
        plans.Select(p => p.Label).Should().OnlyHaveUniqueItems();
    }

}

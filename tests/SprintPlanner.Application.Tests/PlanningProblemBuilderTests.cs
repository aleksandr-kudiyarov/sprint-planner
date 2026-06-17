using FluentAssertions;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Application.Planning;
using SprintPlanner.Application.Statistics;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Domain.ValueObjects;

namespace SprintPlanner.Application.Tests;

public class PlanningProblemBuilderTests
{
    private readonly PlanningProblemBuilder _builder = new(new StatisticsCalculator(new GlobalSettings()));
    private readonly GlobalSettings _settings = new();

    private PlanningProblemRequest Request(
        IReadOnlyList<Developer> devs,
        IReadOnlyList<BacklogItem> tasks,
        decimal riskAppetite = 0.5m) => new()
    {
        SprintStart = new DateOnly(2026, 6, 15), // Monday
        SprintEnd = new DateOnly(2026, 6, 19),   // Friday
        Developers = devs,
        Tasks = tasks,
        Settings = _settings,
        Weights = OptimizationWeights.Balance,
        RiskAppetite = riskAppetite
    };

    [Fact]
    public void Build_AppliesEffectiveWorkCoefficient_ToCapacity()
    {
        var dev = new Developer { Name = "Ann", CapacityHoursPerDay = 6m, IsActive = true };
        var problem = _builder.Build(Request(new[] { dev }, Array.Empty<BacklogItem>()));

        // 5 working days × 6h × 0.8 = 24.
        problem.Developers.Single().EffectiveCapacity.Should().Be(24m);
    }

    [Fact]
    public void Build_ExcludesInactiveDevelopers()
    {
        var active = new Developer { Name = "Ann", CapacityHoursPerDay = 6m, IsActive = true };
        var inactive = new Developer { Name = "Bob", CapacityHoursPerDay = 6m, IsActive = false };

        var problem = _builder.Build(Request(new[] { active, inactive }, Array.Empty<BacklogItem>()));

        problem.Developers.Should().ContainSingle().Which.Name.Should().Be("Ann");
    }

    [Fact]
    public void Build_MapsRiskAppetite_ToGamma()
    {
        var dev = new Developer { Name = "Ann", CapacityHoursPerDay = 6m, IsActive = true };

        var riskAverse = _builder.Build(Request(new[] { dev }, Array.Empty<BacklogItem>(), riskAppetite: 0m));
        var riskSeeking = _builder.Build(Request(new[] { dev }, Array.Empty<BacklogItem>(), riskAppetite: 1m));

        riskAverse.Gamma.Should().Be(PlanningProblemBuilder.GammaMax); // (1-0) × 2
        riskSeeking.Gamma.Should().Be(0m);                              // (1-1) × 2
    }

    [Fact]
    public void Build_UsesLastDeveloper_AsPreferred_WhenNoExplicitPreference()
    {
        var lastDev = Guid.NewGuid();
        var task = new BacklogItem
        {
            Title = "T",
            EstimatedHours = 5m,
            Type = BacklogItemType.Feature,
            LastDeveloperId = lastDev
        };

        var problem = _builder.Build(Request(Array.Empty<Developer>(), new[] { task }));

        problem.Tasks.Single().PreferredDeveloperId.Should().Be(lastDev);
    }

    [Fact]
    public void Build_DropsDependencies_OutsideSelectedSet()
    {
        var external = Guid.NewGuid();
        var dep = new BacklogItem { Title = "Dep", EstimatedHours = 5m };
        var task = new BacklogItem
        {
            Title = "T",
            EstimatedHours = 5m,
            DependencyIds = new List<Guid> { dep.Id, external }
        };

        var problem = _builder.Build(Request(Array.Empty<Developer>(), new[] { dep, task }));

        var built = problem.Tasks.Single(t => t.TaskId == task.Id);
        built.DependencyIds.Should().ContainSingle().Which.Should().Be(dep.Id);
    }

    [Fact]
    public void Build_UsesColdStartSigma_WhenNoHistory()
    {
        var task = new BacklogItem { Title = "T", EstimatedHours = 10m, Type = BacklogItemType.Feature };
        var problem = _builder.Build(Request(Array.Empty<Developer>(), new[] { task }));

        problem.Tasks.Single().Sigma.Should().Be(3.0m); // 0.3 × 10
    }
}

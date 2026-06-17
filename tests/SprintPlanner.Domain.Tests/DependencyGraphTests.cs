using FluentAssertions;
using SprintPlanner.Domain.Services;

namespace SprintPlanner.Domain.Tests;

public class DependencyGraphTests
{
    [Fact]
    public void TopologicalOrder_OrdersDependenciesFirst()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        // b depends on a, c depends on b.
        var graph = new DependencyGraph(
            new[] { a, b, c },
            new[] { (b, a), (c, b) });

        var order = graph.TopologicalOrder()?.ToList();

        order.Should().NotBeNull();
        order!.IndexOf(a).Should().BeLessThan(order.IndexOf(b));
        order.IndexOf(b).Should().BeLessThan(order.IndexOf(c));
    }

    [Fact]
    public void IsAcyclic_DetectsCycle()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var graph = new DependencyGraph(
            new[] { a, b },
            new[] { (a, b), (b, a) });

        graph.IsAcyclic().Should().BeFalse();
        graph.TopologicalOrder().Should().BeNull();
    }

    [Fact]
    public void Constructor_IgnoresEdgesToUnknownNodes()
    {
        var a = Guid.NewGuid();
        var external = Guid.NewGuid();
        var graph = new DependencyGraph(
            new[] { a },
            new[] { (a, external) });

        graph.DependenciesOf(a).Should().BeEmpty();
        graph.IsAcyclic().Should().BeTrue();
    }

    [Fact]
    public void Constructor_IgnoresSelfLoops()
    {
        var a = Guid.NewGuid();
        var graph = new DependencyGraph(new[] { a }, new[] { (a, a) });

        graph.DependenciesOf(a).Should().BeEmpty();
        graph.IsAcyclic().Should().BeTrue();
    }
}

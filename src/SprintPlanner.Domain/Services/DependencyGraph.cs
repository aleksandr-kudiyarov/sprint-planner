namespace SprintPlanner.Domain.Services;

/// <summary>
/// A directed graph of task dependencies. An edge A → B means "A must precede B"
/// (B depends on A). Used to validate acyclicity (F1.3) and to drive critical-path
/// computation (F3.5). Edges that reference unknown nodes are ignored, so callers may
/// pass a subset of the backlog (e.g. only the tasks selected for a sprint).
/// </summary>
public sealed class DependencyGraph
{
    private readonly HashSet<Guid> _nodes;

    // dependency: node -> set of nodes it depends on (predecessors).
    private readonly Dictionary<Guid, HashSet<Guid>> _dependsOn;

    public DependencyGraph(IEnumerable<Guid> nodes, IEnumerable<(Guid Item, Guid DependsOn)> edges)
    {
        _nodes = nodes.ToHashSet();
        _dependsOn = _nodes.ToDictionary(n => n, _ => new HashSet<Guid>());

        foreach (var (item, dependsOn) in edges)
        {
            // Ignore edges that point outside the provided node set.
            if (_nodes.Contains(item) && _nodes.Contains(dependsOn) && item != dependsOn)
            {
                _dependsOn[item].Add(dependsOn);
            }
        }
    }

    public IReadOnlyCollection<Guid> Nodes => _nodes;

    public IReadOnlyCollection<Guid> DependenciesOf(Guid node)
        => _dependsOn.TryGetValue(node, out var deps) ? deps : Array.Empty<Guid>();

    /// <summary>
    /// Returns a topological order (dependencies before dependents) or <c>null</c> if the
    /// graph contains a cycle. Implemented with Kahn's algorithm.
    /// </summary>
    public IReadOnlyList<Guid>? TopologicalOrder()
    {
        var indegree = _nodes.ToDictionary(n => n, n => _dependsOn[n].Count);
        var queue = new Queue<Guid>(indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var order = new List<Guid>(_nodes.Count);

        // dependents: predecessor -> nodes that depend on it.
        var dependents = _nodes.ToDictionary(n => n, _ => new List<Guid>());
        foreach (var node in _nodes)
        {
            foreach (var dep in _dependsOn[node])
            {
                dependents[dep].Add(node);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            order.Add(current);
            foreach (var dependent in dependents[current])
            {
                if (--indegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        return order.Count == _nodes.Count ? order : null;
    }

    public bool IsAcyclic() => TopologicalOrder() is not null;

    /// <summary>
    /// Longest path through the DAG where each node contributes <paramref name="weight"/>
    /// (e.g. a task's adjusted hours). Returns the total weight and the set of nodes on a
    /// maximum-weight path — the critical path (F3.5). Returns (0, empty) when the graph
    /// is empty; throws if the graph has a cycle.
    /// </summary>
    public (decimal Length, IReadOnlyCollection<Guid> Nodes) LongestPathByWeight(Func<Guid, decimal> weight)
    {
        var order = TopologicalOrder()
            ?? throw new InvalidOperationException("Cannot compute critical path on a cyclic graph.");

        // best[n] = max weight of a path ending at n; prev[n] = predecessor on that path.
        var best = new Dictionary<Guid, decimal>();
        var prev = new Dictionary<Guid, Guid?>();

        foreach (var node in order)
        {
            var nodeWeight = weight(node);
            var bestPred = decimal.MinValue;
            Guid? bestPredNode = null;

            foreach (var dep in _dependsOn[node])
            {
                if (best[dep] > bestPred)
                {
                    bestPred = best[dep];
                    bestPredNode = dep;
                }
            }

            best[node] = nodeWeight + (bestPredNode is null ? 0m : bestPred);
            prev[node] = bestPredNode;
        }

        if (best.Count == 0)
        {
            return (0m, Array.Empty<Guid>());
        }

        var end = best.OrderByDescending(kv => kv.Value).First().Key;
        var pathNodes = new List<Guid>();
        Guid? cursor = end;
        while (cursor is not null)
        {
            pathNodes.Add(cursor.Value);
            cursor = prev[cursor.Value];
        }

        pathNodes.Reverse();
        return (best[end], pathNodes);
    }
}

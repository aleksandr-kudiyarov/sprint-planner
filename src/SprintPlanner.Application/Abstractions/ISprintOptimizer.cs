using SprintPlanner.Application.Planning;
using SprintPlanner.Domain.Entities;

namespace SprintPlanner.Application.Abstractions;

/// <summary>
/// Generates a Pareto front of sprint-plan variants for a planning problem (F3.3).
/// Implementations must always return the best solution found, even on solver timeout.
/// </summary>
public interface ISprintOptimizer
{
    /// <summary>
    /// Produces 3–5 distinct plan variants. Each returned <see cref="SprintPlan"/> has its
    /// assignments, metrics and warnings populated but no <c>SprintId</c> set yet
    /// (the caller attaches it).
    /// </summary>
    IReadOnlyList<SprintPlan> Generate(PlanningProblem problem);
}

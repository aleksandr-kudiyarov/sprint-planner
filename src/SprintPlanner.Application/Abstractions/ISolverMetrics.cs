namespace SprintPlanner.Application.Abstractions;

/// <summary>
/// Observability hook for the optimization engine (NFR 5.5). Implemented in the API with
/// Prometheus; a no-op default keeps the Application layer testable.
/// </summary>
public interface ISolverMetrics
{
    /// <summary>Records the wall-clock time of one plan-generation session, in seconds.</summary>
    void RecordGeneration(double seconds, int variantsProduced);
}

public sealed class NullSolverMetrics : ISolverMetrics
{
    public void RecordGeneration(double seconds, int variantsProduced)
    {
    }
}

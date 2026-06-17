using Prometheus;
using SprintPlanner.Application.Abstractions;

namespace SprintPlanner.Api.Infrastructure;

/// <summary>
/// Exports optimization-engine timings to Prometheus (NFR 5.5): a histogram of
/// generation latency and a gauge of the last run, plus a counter of variants produced.
/// </summary>
public sealed class PrometheusSolverMetrics : ISolverMetrics
{
    private static readonly Histogram GenerationDuration = Metrics.CreateHistogram(
        "sprintplanner_solver_generation_seconds",
        "Wall-clock duration of a plan-generation session.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.1, 2, 10)
        });

    private static readonly Gauge LastGenerationDuration = Metrics.CreateGauge(
        "sprintplanner_solver_last_generation_seconds",
        "Duration of the most recent plan-generation session.");

    private static readonly Counter VariantsProduced = Metrics.CreateCounter(
        "sprintplanner_solver_variants_total",
        "Total number of plan variants produced.");

    public void RecordGeneration(double seconds, int variantsProduced)
    {
        GenerationDuration.Observe(seconds);
        LastGenerationDuration.Set(seconds);
        VariantsProduced.Inc(variantsProduced);
    }
}

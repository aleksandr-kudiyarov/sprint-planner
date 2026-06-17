using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintPlanner.Application.Abstractions;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Application.Planning;
using SprintPlanner.Application.Statistics;

namespace SprintPlanner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // The single settings row, loaded once per request scope.
        services.AddScoped(sp =>
        {
            var db = sp.GetRequiredService<IAppDbContext>();
            return db.GlobalSettings.AsNoTracking().FirstOrDefault() ?? new GlobalSettings();
        });

        services.AddScoped<StatisticsCalculator>();
        services.AddScoped<PlanningProblemBuilder>();
        services.AddScoped<PlanMetricsCalculator>();
        services.AddScoped<PlanningService>();

        // Default no-op metrics; the API replaces this with a Prometheus-backed implementation.
        services.AddSingleton<ISolverMetrics, NullSolverMetrics>();
        return services;
    }
}

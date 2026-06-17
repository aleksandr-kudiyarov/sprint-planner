using Microsoft.Extensions.DependencyInjection;
using SprintPlanner.Application.Abstractions;

namespace SprintPlanner.Optimization;

public static class DependencyInjection
{
    public static IServiceCollection AddOptimization(this IServiceCollection services)
    {
        services.AddScoped<ISprintOptimizer, OrToolsSprintOptimizer>();
        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintPlanner.Application.Abstractions;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }
}

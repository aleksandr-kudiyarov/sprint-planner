using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Application.Planning;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class PlanningEndpoints
{
    public static void MapPlanningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sprints/{sprintId:guid}/plans").WithTags("Planning");

        // Generate a Pareto front of plan variants (F3).
        group.MapPost("/generate", async (
            Guid sprintId, GeneratePlansRequest req, PlanningService planning, CancellationToken ct) =>
        {
            try
            {
                var plans = await planning.GeneratePlansAsync(
                    sprintId, req.TaskIds, req.Weights.ToDomain(), req.RiskAppetite, ct);
                return Results.Ok(plans.Select(p => p.ToDto()));
            }
            catch (PlanningValidationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/", async (Guid sprintId, AppDbContext db, CancellationToken ct) =>
        {
            var plans = await db.SprintPlans
                .Include(p => p.Assignments)
                .AsNoTracking()
                .Where(p => p.SprintId == sprintId)
                .ToListAsync(ct);
            return Results.Ok(plans.Select(p => p.ToDto()));
        });

        group.MapGet("/{planId:guid}", async (Guid sprintId, Guid planId, AppDbContext db, CancellationToken ct) =>
        {
            var plan = await db.SprintPlans
                .Include(p => p.Assignments)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId && p.SprintId == sprintId, ct);
            return plan is null ? Results.NotFound() : Results.Ok(plan.ToDto());
        });

        // Approve a plan (F4.4).
        group.MapPost("/{planId:guid}/select", async (
            Guid sprintId, Guid planId, PlanningService planning, CancellationToken ct) =>
        {
            try
            {
                await planning.SelectPlanAsync(sprintId, planId, ct);
                return Results.NoContent();
            }
            catch (PlanningValidationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        // Manual drag & drop reassignment with immediate metric recompute (F4.3).
        group.MapPost("/{planId:guid}/reassign", async (
            Guid sprintId, Guid planId, ReassignTaskRequest req, PlanningService planning, CancellationToken ct) =>
        {
            try
            {
                var plan = await planning.ReassignTaskAsync(planId, req.TaskId, req.NewDeveloperId, ct);
                return Results.Ok(plan.ToDto());
            }
            catch (PlanningValidationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });
    }
}

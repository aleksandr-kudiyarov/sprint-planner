using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class SprintEndpoints
{
    public static void MapSprintEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sprints").WithTags("Sprints");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Sprints.AsNoTracking().OrderByDescending(s => s.StartDate).ToListAsync(ct))
                .Select(s => s.ToDto())));

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var sprint = await db.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
            return sprint is null ? Results.NotFound() : Results.Ok(sprint.ToDto());
        });

        group.MapPost("/", async (UpsertSprintRequest req, AppDbContext db, CancellationToken ct) =>
        {
            if (req.EndDate < req.StartDate)
            {
                return Results.BadRequest("End date must not precede start date.");
            }

            var sprint = new Sprint
            {
                Name = req.Name,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
                Goal = req.Goal,
                Status = SprintStatus.Planning
            };
            db.Sprints.Add(sprint);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/sprints/{sprint.Id}", sprint.ToDto());
        });

        group.MapPut("/{id:guid}", async (Guid id, UpsertSprintRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (sprint is null)
            {
                return Results.NotFound();
            }

            sprint.Name = req.Name;
            sprint.StartDate = req.StartDate;
            sprint.EndDate = req.EndDate;
            sprint.Goal = req.Goal;
            await db.SaveChangesAsync(ct);
            return Results.Ok(sprint.ToDto());
        });

        // Activate the sprint once a plan is committed.
        group.MapPost("/{id:guid}/activate", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (sprint is null)
            {
                return Results.NotFound();
            }

            if (sprint.SelectedPlanId is null)
            {
                return Results.BadRequest("Cannot activate a sprint without a selected plan.");
            }

            sprint.Status = SprintStatus.Active;
            await db.SaveChangesAsync(ct);
            return Results.Ok(sprint.ToDto());
        });
    }
}

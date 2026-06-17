using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Services;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class BacklogEndpoints
{
    public static void MapBacklogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/backlog").WithTags("Backlog");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var items = await db.BacklogItems
                .Include(b => b.RequiredCompetencies)
                .AsNoTracking()
                .ToListAsync(ct);
            return Results.Ok(items.Select(b => b.ToDto()));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.BacklogItems
                .Include(b => b.RequiredCompetencies)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item.ToDto());
        });

        group.MapPost("/", async (UpsertBacklogItemRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var item = new BacklogItem();
            Apply(item, req);

            var error = await ValidateAsync(item, db, ct);
            if (error is not null)
            {
                return Results.BadRequest(error);
            }

            db.BacklogItems.Add(item);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/backlog/{item.Id}", item.ToDto());
        });

        group.MapPut("/{id:guid}", async (Guid id, UpsertBacklogItemRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.BacklogItems
                .Include(b => b.RequiredCompetencies)
                .FirstOrDefaultAsync(b => b.Id == id, ct);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.RequiredCompetencies.Clear();
            Apply(item, req);

            var error = await ValidateAsync(item, db, ct);
            if (error is not null)
            {
                return Results.BadRequest(error);
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(item.ToDto());
        });

        // Toggle the hard lock (Q5). Warns (does not block) if the dev lacks the skill.
        group.MapPut("/{id:guid}/lock", async (Guid id, LockRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.BacklogItems
                .Include(b => b.RequiredCompetencies)
                .FirstOrDefaultAsync(b => b.Id == id, ct);
            if (item is null)
            {
                return Results.NotFound();
            }

            item.LockedDeveloperId = req.DeveloperId;

            string? warning = null;
            if (req.DeveloperId is { } devId && item.RequiredCompetencies.Count > 0)
            {
                var dev = await db.Developers.Include(d => d.Competencies)
                    .FirstOrDefaultAsync(d => d.Id == devId, ct);
                if (dev is not null && !item.RequiredCompetencies.All(r =>
                    dev.Competencies.Any(c => c.CompetencyId == r.CompetencyId && c.Level >= r.MinimumLevel)))
                {
                    warning = "Locked developer does not meet the task's skill requirements.";
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { item = item.ToDto(), warning });
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var item = await db.BacklogItems.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (item is null)
            {
                return Results.NotFound();
            }

            db.BacklogItems.Remove(item);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static void Apply(BacklogItem item, UpsertBacklogItemRequest req)
    {
        item.ExternalId = req.ExternalId;
        item.Title = req.Title;
        item.Description = req.Description;
        item.Type = req.Type;
        item.Priority = req.Priority;
        item.BusinessValue = req.BusinessValue;
        item.EstimatedHours = req.EstimatedHours;
        item.Status = req.Status;
        item.PreferredDeveloperId = req.PreferredDeveloperId;
        item.LockedDeveloperId = req.LockedDeveloperId;
        item.DependencyIds = (req.DependencyIds ?? Array.Empty<Guid>()).ToList();
        item.Tags = (req.Tags ?? Array.Empty<string>()).ToList();
        item.RequiredCompetencies = (req.RequiredCompetencies ?? Array.Empty<CompetencyRequirementDto>())
            .Select(r => new CompetencyRequirement
            {
                BacklogItemId = item.Id,
                CompetencyId = r.CompetencyId,
                MinimumLevel = r.MinimumLevel
            })
            .ToList();
    }

    /// <summary>Validates that dependencies exist and introduce no cycle (F1.3).</summary>
    private static async Task<string?> ValidateAsync(BacklogItem item, AppDbContext db, CancellationToken ct)
    {
        if (item.EstimatedHours <= 0)
        {
            return "Estimated hours must be positive.";
        }

        if (item.DependencyIds.Count == 0)
        {
            return null;
        }

        if (item.DependencyIds.Contains(item.Id))
        {
            return "A task cannot depend on itself.";
        }

        var allItems = await db.BacklogItems
            .Select(b => new { b.Id, b.DependencyIds })
            .ToListAsync(ct);

        var nodes = allItems.Select(b => b.Id).Append(item.Id).Distinct().ToList();
        var edges = allItems
            .Where(b => b.Id != item.Id)
            .SelectMany(b => b.DependencyIds.Select(dep => (b.Id, dep)))
            .Concat(item.DependencyIds.Select(dep => (item.Id, dep)))
            .ToList();

        var graph = new DependencyGraph(nodes, edges);
        return graph.IsAcyclic() ? null : "Dependencies would introduce a cycle.";
    }

    private sealed record LockRequest(Guid? DeveloperId);
}

using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class DeveloperEndpoints
{
    public static void MapDeveloperEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/developers").WithTags("Developers");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var devs = await db.Developers
                .Include(d => d.Competencies)
                .Include(d => d.CalendarOverrides)
                .AsNoTracking()
                .ToListAsync(ct);
            return Results.Ok(devs.Select(d => d.ToDto()));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var dev = await db.Developers
                .Include(d => d.Competencies)
                .Include(d => d.CalendarOverrides)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            return dev is null ? Results.NotFound() : Results.Ok(dev.ToDto());
        });

        group.MapPost("/", async (CreateDeveloperRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var dev = new Developer
            {
                Name = req.Name,
                Email = req.Email,
                CapacityHoursPerDay = req.CapacityHoursPerDay,
                IsActive = true
            };
            db.Developers.Add(dev);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/developers/{dev.Id}", dev.ToDto());
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateDeveloperRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var dev = await db.Developers
                .Include(d => d.CalendarOverrides)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (dev is null)
            {
                return Results.NotFound();
            }

            dev.Name = req.Name;
            dev.Email = req.Email;
            dev.CapacityHoursPerDay = req.CapacityHoursPerDay;
            dev.IsActive = req.IsActive;

            if (req.CalendarOverrides is not null)
            {
                dev.CalendarOverrides.Clear();
                foreach (var o in req.CalendarOverrides)
                {
                    dev.CalendarOverrides.Add(new CalendarOverride
                    {
                        DeveloperId = dev.Id,
                        Date = o.Date,
                        AvailableHours = o.AvailableHours
                    });
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(dev.ToDto());
        });

        // Deactivate (soft delete) — developers are never hard-deleted to preserve history.
        group.MapPost("/{id:guid}/deactivate", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var dev = await db.Developers.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (dev is null)
            {
                return Results.NotFound();
            }

            dev.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // Set a competency level, recording the change in history (F1.2).
        group.MapPut("/{id:guid}/competencies", async (
            Guid id, SetCompetencyLevelRequest req, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var dev = await db.Developers
                .Include(d => d.Competencies)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (dev is null)
            {
                return Results.NotFound();
            }

            if (!await db.Competencies.AnyAsync(c => c.Id == req.CompetencyId, ct))
            {
                return Results.BadRequest("Unknown competency.");
            }

            var updatedBy = http.User.Identity?.Name ?? "tech-lead";
            var existing = dev.Competencies.FirstOrDefault(c => c.CompetencyId == req.CompetencyId);
            CompetencyLevel previous;
            if (existing is null)
            {
                existing = new DeveloperCompetency { DeveloperId = dev.Id, CompetencyId = req.CompetencyId };
                dev.Competencies.Add(existing);
                previous = CompetencyLevel.None;
                existing.ApplyLevel(req.Level, updatedBy, req.LevelNumeric);
            }
            else
            {
                previous = existing.ApplyLevel(req.Level, updatedBy, req.LevelNumeric);
            }

            db.DeveloperCompetencyHistory.Add(new DeveloperCompetencyHistory
            {
                DeveloperId = dev.Id,
                CompetencyId = req.CompetencyId,
                FromLevel = previous,
                ToLevel = req.Level,
                ChangedBy = updatedBy
            });

            await db.SaveChangesAsync(ct);
            return Results.Ok(dev.ToDto());
        });
    }
}

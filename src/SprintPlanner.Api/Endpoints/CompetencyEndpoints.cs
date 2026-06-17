using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class CompetencyEndpoints
{
    public static void MapCompetencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/competencies").WithTags("Competencies");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok((await db.Competencies.AsNoTracking().ToListAsync(ct)).Select(c => c.ToDto())));

        group.MapPost("/", async (CreateCompetencyRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var competency = new Competency { Name = req.Name, Description = req.Description };
            db.Competencies.Add(competency);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/competencies/{competency.Id}", competency.ToDto());
        });

        group.MapPut("/{id:guid}", async (Guid id, CreateCompetencyRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var competency = await db.Competencies.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (competency is null)
            {
                return Results.NotFound();
            }

            competency.Name = req.Name;
            competency.Description = req.Description;
            await db.SaveChangesAsync(ct);
            return Results.Ok(competency.ToDto());
        });
    }
}

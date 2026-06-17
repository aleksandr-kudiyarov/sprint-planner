using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Application.Statistics;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class StatisticsEndpoints
{
    public static void MapStatisticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/statistics").WithTags("Statistics");

        // Per-developer velocity / β / trend (F2).
        group.MapGet("/developers", async (
            AppDbContext db, StatisticsCalculator calc, CancellationToken ct) =>
        {
            var history = await LoadHistoryAsync(db, ct);
            var chronology = await db.Sprints
                .Where(s => s.Status == SprintStatus.Completed)
                .OrderBy(s => s.StartDate)
                .Select(s => s.Id)
                .ToListAsync(ct);

            var developers = await db.Developers.AsNoTracking().Select(d => d.Id).ToListAsync(ct);
            var result = developers
                .Select(id => calc.ComputeForDeveloper(id, history, chronology).ToDto())
                .ToList();
            return Results.Ok(result);
        });

        group.MapGet("/developers/{id:guid}", async (
            Guid id, AppDbContext db, StatisticsCalculator calc, CancellationToken ct) =>
        {
            var history = await LoadHistoryAsync(db, ct);
            var chronology = await db.Sprints
                .Where(s => s.Status == SprintStatus.Completed)
                .OrderBy(s => s.StartDate)
                .Select(s => s.Id)
                .ToListAsync(ct);
            return Results.Ok(calc.ComputeForDeveloper(id, history, chronology).ToDto());
        });
    }

    private static async Task<IReadOnlyList<CompletedTaskRecord>> LoadHistoryAsync(AppDbContext db, CancellationToken ct)
    {
        var query =
            from h in db.SprintHistory.AsNoTracking()
            join b in db.BacklogItems on h.TaskId equals b.Id into bj
            from b in bj.DefaultIfEmpty()
            select new { h, Type = b != null ? b.Type : BacklogItemType.Feature };

        var rows = await query.ToListAsync(ct);
        return rows
            .Select(r => new CompletedTaskRecord(
                r.h.SprintId, r.h.DeveloperId, r.Type, r.h.EstimatedHours, r.h.ActualHours, r.h.WasCompleted))
            .ToList();
    }
}

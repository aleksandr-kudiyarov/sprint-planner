using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Domain.Entities;
using SprintPlanner.Domain.Enums;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sprints/{sprintId:guid}").WithTags("Feedback");

        // Record actuals, complete the sprint, return carried-over tasks to backlog (F5.1).
        group.MapPost("/feedback", async (
            Guid sprintId, SprintFeedbackRequest req, AppDbContext db, CancellationToken ct) =>
        {
            var sprint = await db.Sprints.FirstOrDefaultAsync(s => s.Id == sprintId, ct);
            if (sprint is null)
            {
                return Results.NotFound();
            }

            var taskIds = req.Items.Select(i => i.TaskId).ToHashSet();
            var tasks = await db.BacklogItems.Where(b => taskIds.Contains(b.Id)).ToListAsync(ct);
            var taskById = tasks.ToDictionary(t => t.Id);

            foreach (var item in req.Items)
            {
                if (!taskById.TryGetValue(item.TaskId, out var task))
                {
                    continue;
                }

                db.SprintHistory.Add(new SprintHistory
                {
                    SprintId = sprintId,
                    TaskId = item.TaskId,
                    DeveloperId = item.DeveloperId,
                    EstimatedHours = task.EstimatedHours,
                    ActualHours = item.ActualHours,
                    WasCompleted = item.WasCompleted,
                    CompletedAt = item.WasCompleted ? DateTime.UtcNow : null,
                    Notes = item.Notes ?? string.Empty
                });

                // Remember who worked on it (continuity for next sprint, F1.3).
                task.LastDeveloperId = item.DeveloperId;

                // Carried-over tasks go back to the backlog (F5.1); completed ones are done.
                task.Status = item.WasCompleted ? BacklogItemStatus.Done : BacklogItemStatus.ReadyForPlanning;
            }

            sprint.Status = SprintStatus.Completed;
            await db.SaveChangesAsync(ct);

            return Results.Ok(BuildReport(sprintId, req));
        });

        // Post-sprint report (F5.2).
        group.MapGet("/report", async (Guid sprintId, AppDbContext db, CancellationToken ct) =>
        {
            var history = await db.SprintHistory.AsNoTracking()
                .Where(h => h.SprintId == sprintId)
                .ToListAsync(ct);
            if (history.Count == 0)
            {
                return Results.NotFound();
            }

            var items = history
                .Select(h => new SprintFeedbackItem(h.TaskId, h.DeveloperId, h.ActualHours, h.WasCompleted, h.Notes))
                .ToList();
            var estimateByTask = history.ToDictionary(h => h.TaskId, h => h.EstimatedHours);
            return Results.Ok(BuildReport(sprintId, new SprintFeedbackRequest(items), estimateByTask));
        });
    }

    private static SprintReportDto BuildReport(
        Guid sprintId,
        SprintFeedbackRequest req,
        IReadOnlyDictionary<Guid, decimal>? estimateByTask = null)
    {
        var perDeveloper = req.Items
            .GroupBy(i => i.DeveloperId)
            .Select(g =>
            {
                var actual = g.Sum(i => i.ActualHours);
                var estimate = estimateByTask is null
                    ? 0m
                    : g.Sum(i => estimateByTask.TryGetValue(i.TaskId, out var e) ? e : 0m);
                var deviation = estimate > 0 ? (actual - estimate) / estimate : 0m;
                return new DeveloperReportLine(g.Key, estimate, actual, deviation, g.Count(i => i.WasCompleted));
            })
            .ToList();

        var teamVelocity = req.Items.Where(i => i.WasCompleted).Sum(i => i.ActualHours);
        var completed = req.Items.Count(i => i.WasCompleted);
        var carriedOver = req.Items.Count(i => !i.WasCompleted);

        return new SprintReportDto(sprintId, teamVelocity, completed, carriedOver, perDeveloper);
    }

    private sealed record DeveloperReportLine(
        Guid DeveloperId, decimal EstimatedHours, decimal ActualHours, decimal Deviation, int CompletedTasks);

    private sealed record SprintReportDto(
        Guid SprintId, decimal TeamVelocity, int CompletedTasks, int CarriedOverTasks,
        IReadOnlyList<DeveloperReportLine> PerDeveloper);
}

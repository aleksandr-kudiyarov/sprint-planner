using Microsoft.EntityFrameworkCore;
using SprintPlanner.Api.Contracts;
using SprintPlanner.Application.Configuration;
using SprintPlanner.Infrastructure.Persistence;

namespace SprintPlanner.Api.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var settings = await db.GlobalSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new GlobalSettings();
            return Results.Ok(settings.ToDto());
        });

        group.MapPut("/", async (GlobalSettingsDto dto, AppDbContext db, CancellationToken ct) =>
        {
            var settings = await db.GlobalSettings.FirstOrDefaultAsync(ct);
            if (settings is null)
            {
                settings = new GlobalSettings();
                db.GlobalSettings.Add(settings);
            }

            settings.DefaultSprintLengthDays = dto.DefaultSprintLengthDays;
            settings.EffectiveWorkCoefficient = dto.EffectiveWorkCoefficient;
            settings.VelocityWindowSprints = dto.VelocityWindowSprints;
            settings.LoadWarningThreshold = dto.LoadWarningThreshold;
            settings.MinSprintsForReliableStats = dto.MinSprintsForReliableStats;
            settings.ColdStartBeta = dto.ColdStartBeta;
            settings.ColdStartSigmaFraction = dto.ColdStartSigmaFraction;

            await db.SaveChangesAsync(ct);
            return Results.Ok(settings.ToDto());
        });
    }
}

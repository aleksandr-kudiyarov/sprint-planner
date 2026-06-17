using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using SprintPlanner.Api.Endpoints;
using SprintPlanner.Api.Infrastructure;
using SprintPlanner.Application;
using SprintPlanner.Application.Abstractions;
using SprintPlanner.Infrastructure;
using SprintPlanner.Infrastructure.Persistence;
using SprintPlanner.Optimization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? "Host=localhost;Port=5432;Database=sprintplanner;Username=postgres;Password=postgres";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplication();
builder.Services.AddOptimization();

// Replace the no-op solver metrics with the Prometheus-backed implementation (NFR 5.5).
builder.Services.AddSingleton<ISolverMetrics, PrometheusSolverMetrics>();

// Authentication via Keycloak (OIDC/JWT). When no authority is configured we run open
// for local development and log a warning; production must set Authentication:Authority.
var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
var authEnabled = !string.IsNullOrWhiteSpace(authority);

if (authEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(audience);
        });

    // Single role (TL): every endpoint requires an authenticated user (NFR 5.3).
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());
}
else
{
    builder.Services.AddAuthorization();
}

// Serialize/accept enums as strings for a friendlier, frontend-stable contract.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
          .AllowAnyHeader()
          .AllowAnyMethod()));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (!authEnabled)
{
    app.Logger.LogWarning(
        "Authentication:Authority is not configured — the API is running WITHOUT authentication (development only).");
}

// Apply migrations on startup for single-instance deployments.
if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Prometheus endpoint-latency metrics + scrape endpoint (NFR 5.5).
app.UseHttpMetrics();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Kubernetes probes (NFR 5.5). Health endpoints stay anonymous.
app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapMetrics().AllowAnonymous();

app.MapDeveloperEndpoints();
app.MapCompetencyEndpoints();
app.MapBacklogEndpoints();
app.MapSprintEndpoints();
app.MapPlanningEndpoints();
app.MapFeedbackEndpoints();
app.MapStatisticsEndpoints();
app.MapSettingsEndpoints();

app.Run();

public partial class Program;

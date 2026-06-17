# Sprint Planner

A Tech Lead's tool for mathematically optimized distribution of backlog items across
developers within a sprint. It formalizes data about tasks, developers and sprint
history, runs an ILP optimization engine (Google OR-Tools), and presents the TL with a
set of ready-made plan variants on a Pareto front — each with explicit trade-offs — so
the human makes an informed choice. See `docs/requirements.md` for the full spec.

> The system **assists** planning; it does not replace the planning meeting, is not a
> task tracker, and does not manage a running sprint.

## Architecture

The backend follows **Clean Architecture** with dependencies pointing inward:

```
SprintPlanner.Api            ASP.NET Core 8 host: endpoints, auth (Keycloak/JWT),
                             health checks, Prometheus metrics.
SprintPlanner.Optimization   OR-Tools ILP model: objective, constraints, Pareto fronts,
                             critical path. Depends on Domain + Application.
SprintPlanner.Infrastructure EF Core + PostgreSQL persistence, repositories.
SprintPlanner.Application    Use-cases, DTOs, statistics (velocity, β, σ, cold start).
SprintPlanner.Domain         Entities, enums, value objects, pure domain services.
                             No external dependencies.
frontend/                    React + TypeScript SPA (plan comparison, radar chart,
                             drag & drop board).
```

### Why these choices

- **Clean Architecture** keeps the domain (the optimization model's vocabulary) free of
  framework concerns, which matters because the solver logic is the riskiest, most
  test-worthy part of the system.
- **Hours as the only estimation unit** (ADR Q1) — story points are deliberately absent.
- **Cold start** (ADR Q4): with no history, `β = 1.0`, `σ = 0.3 × estimate`, capacity =
  `nominal × 0.8`. The UI flags "insufficient data" until 3 sprints accumulate.
- **OR-Tools (CP-SAT/SCIP)** for the ILP, time-boxed to 10s per generation, always
  returning the best solution found even on timeout.

## Tech stack

ASP.NET Core 8 · PostgreSQL (EF Core) · Google.OrTools · React + TypeScript ·
Keycloak (OIDC/JWT) · Docker / Kubernetes · Prometheus + Grafana.

## Building

```bash
dotnet build SprintPlanner.sln
dotnet test                       # 45 unit tests (domain, statistics, optimizer)
```

## Running

### Full stack (Docker)

```bash
docker compose up --build
# API      → http://localhost:5080  (Swagger at /swagger in Development)
# Frontend → http://localhost:5173
# Prometheus → http://localhost:9090
# Keycloak  → http://localhost:8081
```

### Local development

```bash
# 1. PostgreSQL on :5432 (database "sprintplanner")
# 2. API — applies migrations on startup
dotnet run --project src/SprintPlanner.Api      # http://localhost:5080
# 3. Frontend (proxies /api to the API)
cd frontend && npm install && npm run dev       # http://localhost:5173
```

Authentication is enforced only when `Authentication:Authority` (a Keycloak realm
URL) is configured; otherwise the API runs open for local development and logs a
warning. `/healthz` and `/metrics` are always anonymous.

## Requirements coverage

| Area | Where |
|---|---|
| Domain model (§3) | `SprintPlanner.Domain` — entities, enums, value objects |
| Effective capacity, calendar overrides (F1.1) | `Developer.EffectiveCapacity` |
| Competency levels + change history (F1.2) | `DeveloperCompetency`, `DeveloperCompetencyHistory` |
| Backlog, dependency DAG validation, lock (F1.3, Q5) | `BacklogEndpoints`, `DependencyGraph` |
| Velocity, β, σ, trend, cold start (F2) | `StatisticsCalculator` |
| ILP optimizer, robust capacity, constraints (F3.2) | `OrToolsSprintOptimizer` |
| Pareto front (F3.3) | weight-profile sweep in the optimizer |
| Uncertainty / overload probability (F3.4) | `PlanMetricsCalculator` |
| Critical path (F3.5) | `DependencyGraph.LongestPathByWeight` |
| Plan comparison, radar, board, drag & drop (F4) | `frontend/src/pages/PlanningPage.tsx` |
| Approve plan (F4.4) | `PlanningService.SelectPlanAsync` |
| Feedback & report (F5) | `FeedbackEndpoints` |
| Settings / weight profiles (F6) | `SettingsEndpoints`, `OptimizationWeights` |
| Keycloak JWT, health, Prometheus (5.3–5.5) | `Program.cs`, `PrometheusSolverMetrics` |

## Status

Backend complete and verified end-to-end against PostgreSQL 16 (full TL flow:
create sprint → generate Pareto variants → select → activate → feedback →
statistics, with skill/dependency/capacity constraints all honoured). Frontend
covers the team, backlog and plan-decision screens. See commit history for
per-step progress.


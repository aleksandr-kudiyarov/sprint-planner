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
dotnet test
```

## Status

Implemented iteratively. See commit history for per-step progress.

import type {
  BacklogItem, Competency, Developer, DeveloperStatistics,
  GlobalSettings, OptimizationWeights, Sprint, SprintPlan
} from "./types";

const BASE = "";

async function http<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(BASE + path, {
    method,
    headers: body !== undefined ? { "Content-Type": "application/json" } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `${method} ${path} failed with ${res.status}`);
  }
  if (res.status === 204) {
    return undefined as T;
  }
  const text = await res.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

export const api = {
  // Developers
  listDevelopers: () => http<Developer[]>("GET", "/api/developers"),
  createDeveloper: (d: { name: string; email: string; capacityHoursPerDay: number }) =>
    http<Developer>("POST", "/api/developers", d),
  setCompetency: (devId: string, competencyId: string, level: string) =>
    http<Developer>("PUT", `/api/developers/${devId}/competencies`, { competencyId, level }),

  // Competencies
  listCompetencies: () => http<Competency[]>("GET", "/api/competencies"),
  createCompetency: (c: { name: string; description: string }) =>
    http<Competency>("POST", "/api/competencies", c),

  // Backlog
  listBacklog: () => http<BacklogItem[]>("GET", "/api/backlog"),
  createBacklogItem: (b: Partial<BacklogItem>) => http<BacklogItem>("POST", "/api/backlog", b),
  setLock: (id: string, developerId: string | null) =>
    http<{ item: BacklogItem; warning?: string }>("PUT", `/api/backlog/${id}/lock`, { developerId }),

  // Sprints
  listSprints: () => http<Sprint[]>("GET", "/api/sprints"),
  getSprint: (id: string) => http<Sprint>("GET", `/api/sprints/${id}`),
  createSprint: (s: { name: string; startDate: string; endDate: string; goal: string }) =>
    http<Sprint>("POST", "/api/sprints", s),
  activateSprint: (id: string) => http<Sprint>("POST", `/api/sprints/${id}/activate`),

  // Planning
  generatePlans: (sprintId: string, taskIds: string[], weights: OptimizationWeights, riskAppetite: number) =>
    http<SprintPlan[]>("POST", `/api/sprints/${sprintId}/plans/generate`, { taskIds, weights, riskAppetite }),
  listPlans: (sprintId: string) => http<SprintPlan[]>("GET", `/api/sprints/${sprintId}/plans`),
  selectPlan: (sprintId: string, planId: string) =>
    http<void>("POST", `/api/sprints/${sprintId}/plans/${planId}/select`),
  reassign: (sprintId: string, planId: string, taskId: string, newDeveloperId: string) =>
    http<SprintPlan>("POST", `/api/sprints/${sprintId}/plans/${planId}/reassign`, { taskId, newDeveloperId }),

  // Statistics & settings
  developerStats: () => http<DeveloperStatistics[]>("GET", "/api/statistics/developers"),
  getSettings: () => http<GlobalSettings>("GET", "/api/settings"),
  updateSettings: (s: GlobalSettings) => http<GlobalSettings>("PUT", "/api/settings", s)
};

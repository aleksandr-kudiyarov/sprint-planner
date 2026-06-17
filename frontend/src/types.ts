export type CompetencyLevel = "None" | "Junior" | "Mid" | "Senior" | "Expert";
export type BacklogItemType = "Feature" | "Bug" | "TechDebt" | "Review" | "Chore";
export type BacklogItemStatus = "New" | "ReadyForPlanning" | "InSprint" | "Done";
export type SprintStatus = "Planning" | "Active" | "Completed";

export interface Competency {
  id: string;
  name: string;
  description: string;
}

export interface DeveloperCompetency {
  competencyId: string;
  level: CompetencyLevel;
  levelNumeric: number;
}

export interface CalendarOverride {
  date: string;
  availableHours: number;
}

export interface Developer {
  id: string;
  name: string;
  email: string;
  isActive: boolean;
  capacityHoursPerDay: number;
  calendarOverrides: CalendarOverride[];
  competencies: DeveloperCompetency[];
}

export interface CompetencyRequirement {
  competencyId: string;
  minimumLevel: CompetencyLevel;
}

export interface BacklogItem {
  id: string;
  externalId?: string | null;
  title: string;
  description: string;
  type: BacklogItemType;
  priority: number;
  businessValue: number;
  estimatedHours: number;
  status: BacklogItemStatus;
  preferredDeveloperId?: string | null;
  lockedDeveloperId?: string | null;
  lastDeveloperId?: string | null;
  requiredCompetencies: CompetencyRequirement[];
  dependencyIds: string[];
  tags: string[];
}

export interface Sprint {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
  goal: string;
  selectedPlanId?: string | null;
}

export interface OptimizationWeights {
  businessValue: number;
  loadBalance: number;
  skillFit: number;
  riskMinimization: number;
  continuity: number;
}

export interface TaskAssignment {
  taskId: string;
  developerId: string;
  estimatedHours: number;
  adjustedHours: number;
  continuityBonus: boolean;
  skillFitScore: number;
  isOnCriticalPath: boolean;
}

export interface PlanMetrics {
  totalBusinessValue: number;
  capacityUtilization: Record<string, number>;
  loadBalanceScore: number;
  averageSkillFit: number;
  riskScore: number;
  continuityScore: number;
  criticalPathLength: number;
}

export interface SprintPlan {
  id: string;
  sprintId: string;
  generatedAt: string;
  isSelected: boolean;
  label: string;
  weights: OptimizationWeights;
  assignments: TaskAssignment[];
  metrics: PlanMetrics;
  warnings: string[];
}

export interface DeveloperStatistics {
  developerId: string;
  completedSprintCount: number;
  hasSufficientData: boolean;
  velocity: number | null;
  velocityStdDev: number | null;
  trend: string;
  beta: number;
}

export interface GlobalSettings {
  defaultSprintLengthDays: number;
  effectiveWorkCoefficient: number;
  velocityWindowSprints: number;
  loadWarningThreshold: number;
  minSprintsForReliableStats: number;
  coldStartBeta: number;
  coldStartSigmaFraction: number;
}

export const WEIGHT_PROFILES: Record<string, OptimizationWeights> = {
  Value: { businessValue: 0.5, loadBalance: 0.1, skillFit: 0.2, riskMinimization: 0.1, continuity: 0.1 },
  Stability: { businessValue: 0.2, loadBalance: 0.2, skillFit: 0.2, riskMinimization: 0.3, continuity: 0.1 },
  Balance: { businessValue: 0.2, loadBalance: 0.3, skillFit: 0.2, riskMinimization: 0.2, continuity: 0.1 }
};

import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { api } from "../api";
import { useAsync } from "../useAsync";
import { WEIGHT_PROFILES, type OptimizationWeights, type SprintPlan } from "../types";
import RadarComparison from "../components/RadarComparison";
import PlanBoard from "../components/PlanBoard";

const CRITERIA: (keyof OptimizationWeights)[] = [
  "businessValue", "loadBalance", "skillFit", "riskMinimization", "continuity"
];
const LABELS: Record<keyof OptimizationWeights, string> = {
  businessValue: "Business value", loadBalance: "Load balance", skillFit: "Skill fit",
  riskMinimization: "Risk min.", continuity: "Continuity"
};

export default function PlanningPage() {
  const { sprintId } = useParams();
  const sprint = useAsync(() => api.getSprint(sprintId!), [sprintId]);
  const backlog = useAsync(() => api.listBacklog());
  const devs = useAsync(() => api.listDevelopers());
  const settings = useAsync(() => api.getSettings());

  const [selectedTasks, setSelectedTasks] = useState<Set<string>>(new Set());
  const [weights, setWeights] = useState<OptimizationWeights>(WEIGHT_PROFILES.Balance);
  const [riskAppetite, setRiskAppetite] = useState(0.5);
  const [plans, setPlans] = useState<SprintPlan[]>([]);
  const [activePlanId, setActivePlanId] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const plannable = useMemo(
    () => (backlog.data ?? []).filter((b) => b.status === "ReadyForPlanning" || b.status === "InSprint"),
    [backlog.data]
  );
  const activeDevs = useMemo(() => (devs.data ?? []).filter((d) => d.isActive), [devs.data]);
  const activePlan = plans.find((p) => p.id === activePlanId) ?? null;
  const weightSum = CRITERIA.reduce((s, k) => s + weights[k], 0);

  function toggleTask(id: string) {
    const next = new Set(selectedTasks);
    next.has(id) ? next.delete(id) : next.add(id);
    setSelectedTasks(next);
  }

  async function generate() {
    setBusy(true);
    setError(null);
    try {
      const result = await api.generatePlans(sprintId!, [...selectedTasks], weights, riskAppetite);
      setPlans(result);
      setActivePlanId(result[0]?.id ?? null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  async function reassign(taskId: string, developerId: string) {
    if (!activePlan) return;
    try {
      const updated = await api.reassign(sprintId!, activePlan.id, taskId, developerId);
      setPlans(plans.map((p) => (p.id === updated.id ? updated : p)));
    } catch (e) {
      alert((e as Error).message);
    }
  }

  async function approve() {
    if (!activePlan) return;
    await api.selectPlan(sprintId!, activePlan.id);
    sprint.reload();
    alert(`Plan "${activePlan.label}" approved.`);
  }

  return (
    <div>
      <div className="panel">
        <h2>{sprint.data?.name ?? "Sprint"} — planning</h2>
        <p className="muted">{sprint.data?.startDate} → {sprint.data?.endDate} · {sprint.data?.goal}</p>
      </div>

      <div className="panel">
        <h2>1 · Select tasks</h2>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(280px,1fr))", gap: "0.3rem" }}>
          {plannable.map((b) => (
            <label key={b.id} className="checkbox-row">
              <input type="checkbox" checked={selectedTasks.has(b.id)} onChange={() => toggleTask(b.id)} />
              <span>{b.title} <span className="muted">· {b.estimatedHours}h · BV {b.businessValue}</span>
                {b.lockedDeveloperId && " 🔒"}</span>
            </label>
          ))}
          {plannable.length === 0 && <span className="muted">No plannable backlog items.</span>}
        </div>
      </div>

      <div className="panel">
        <h2>2 · Optimization weights</h2>
        <div className="row" style={{ marginBottom: "0.5rem" }}>
          {Object.keys(WEIGHT_PROFILES).map((name) => (
            <button key={name} className="secondary" onClick={() => setWeights(WEIGHT_PROFILES[name])}>{name}</button>
          ))}
        </div>
        {CRITERIA.map((k) => (
          <div className="weight-row" key={k}>
            <span>{LABELS[k]}</span>
            <input type="range" min={0} max={1} step={0.05} value={weights[k]}
              onChange={(e) => setWeights({ ...weights, [k]: Number(e.target.value) })} />
            <span className="muted">{(weights[k] / (weightSum || 1)).toFixed(2)}</span>
          </div>
        ))}
        <div className="weight-row">
          <span>Risk appetite</span>
          <input type="range" min={0} max={1} step={0.05} value={riskAppetite}
            onChange={(e) => setRiskAppetite(Number(e.target.value))} />
          <span className="muted">{(riskAppetite * 100).toFixed(0)}%</span>
        </div>
        <button onClick={generate} disabled={busy || selectedTasks.size === 0}>
          {busy ? "Optimizing…" : "Generate plans"}
        </button>
        {error && <p className="error">{error}</p>}
      </div>

      {plans.length > 0 && (
        <>
          <div className="panel">
            <h2>3 · Compare variants</h2>
            <div className="plan-grid">
              {plans.map((p) => (
                <div key={p.id} className={`plan-card ${p.id === activePlanId ? "selected" : ""}`}
                  onClick={() => setActivePlanId(p.id)}>
                  <h3>{p.label}</h3>
                  <div className="metric-line"><span>Business value</span><strong>{p.metrics.totalBusinessValue.toFixed(0)}</strong></div>
                  <div className="metric-line"><span>Tasks</span><span>{p.assignments.length}</span></div>
                  <div className="metric-line"><span>Load balance</span><span>{(p.metrics.loadBalanceScore * 100).toFixed(0)}%</span></div>
                  <div className="metric-line"><span>Skill fit</span><span>{(p.metrics.averageSkillFit * 100).toFixed(0)}%</span></div>
                  <div className="metric-line">
                    <span>Overload risk</span>
                    <span className={`badge ${p.metrics.riskScore > 0.3 ? "crit" : p.metrics.riskScore > 0.1 ? "warn" : ""}`}>
                      {(p.metrics.riskScore * 100).toFixed(0)}%
                    </span>
                  </div>
                  <div className="metric-line"><span>Critical path</span><span>{p.metrics.criticalPathLength.toFixed(0)}h</span></div>
                </div>
              ))}
            </div>
            <div style={{ marginTop: "1rem" }}>
              <RadarComparison plans={plans} />
            </div>
          </div>

          {activePlan && (
            <div className="panel">
              <h2>4 · {activePlan.label} — board</h2>
              {activePlan.warnings.length > 0 && (
                <ul className="warnings">
                  {activePlan.warnings.map((w, i) => <li key={i}>{w}</li>)}
                </ul>
              )}
              <p className="muted">Drag a task onto another developer to reassign (locked tasks 🔒 cannot move).</p>
              <PlanBoard
                plan={activePlan}
                developers={activeDevs}
                backlog={backlog.data ?? []}
                warningThreshold={settings.data?.loadWarningThreshold ?? 0.85}
                onReassign={reassign}
              />
              <div style={{ marginTop: "1rem" }}>
                <button onClick={approve}>Approve plan</button>
                {sprint.data?.selectedPlanId === activePlan.id && <span className="badge" style={{ marginLeft: 8 }}>approved</span>}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

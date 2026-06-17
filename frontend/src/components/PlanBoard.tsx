import { useState } from "react";
import type { BacklogItem, Developer, SprintPlan } from "../types";

interface Props {
  plan: SprintPlan;
  developers: Developer[];
  backlog: BacklogItem[];
  warningThreshold: number;
  onReassign: (taskId: string, developerId: string) => void;
}

/** Per-developer board with load bars and HTML5 drag & drop (F4.2 / F4.3). */
export default function PlanBoard({ plan, developers, backlog, warningThreshold, onReassign }: Props) {
  const [dragOver, setDragOver] = useState<string | null>(null);
  const taskById = Object.fromEntries(backlog.map((b) => [b.id, b]));

  function barClass(util: number) {
    if (util > 0.95) return "bar over";
    if (util >= warningThreshold) return "bar warn";
    return "bar ok";
  }

  return (
    <div className="board">
      {developers.map((dev) => {
        const assignments = plan.assignments.filter((a) => a.developerId === dev.id);
        const util = plan.metrics.capacityUtilization[dev.id] ?? 0;
        return (
          <div
            key={dev.id}
            className={`dev-col ${dragOver === dev.id ? "dragover" : ""}`}
            onDragOver={(e) => { e.preventDefault(); setDragOver(dev.id); }}
            onDragLeave={() => setDragOver(null)}
            onDrop={(e) => {
              e.preventDefault();
              setDragOver(null);
              const taskId = e.dataTransfer.getData("text/taskId");
              if (taskId) onReassign(taskId, dev.id);
            }}
          >
            <strong>{dev.name}</strong>
            <div className={barClass(util)} style={{ margin: "0.4rem 0" }}>
              <span style={{ width: `${Math.min(100, util * 100)}%` }} />
            </div>
            <div className="muted" style={{ fontSize: "0.75rem", marginBottom: "0.5rem" }}>
              {(util * 100).toFixed(0)}% of capacity
            </div>

            {assignments.map((a) => {
              const task = taskById[a.taskId];
              const locked = !!task?.lockedDeveloperId;
              return (
                <div
                  key={a.taskId}
                  className={`task-card ${locked ? "locked" : ""}`}
                  draggable={!locked}
                  onDragStart={(e) => e.dataTransfer.setData("text/taskId", a.taskId)}
                >
                  <div className="title">
                    {locked && "🔒 "}{task?.title ?? a.taskId.slice(0, 8)}
                    {a.isOnCriticalPath && <span className="badge crit" style={{ marginLeft: 6 }}>critical</span>}
                  </div>
                  <div className="task-meta">
                    <span>{a.adjustedHours.toFixed(1)}h (est {a.estimatedHours})</span>
                    <span>fit {(a.skillFitScore * 100).toFixed(0)}%</span>
                    {a.continuityBonus && <span className="badge">continuity</span>}
                  </div>
                </div>
              );
            })}
            {assignments.length === 0 && <div className="muted" style={{ fontSize: "0.8rem" }}>No tasks</div>}
          </div>
        );
      })}
    </div>
  );
}

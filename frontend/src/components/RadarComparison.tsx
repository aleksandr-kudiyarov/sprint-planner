import {
  Radar, RadarChart, PolarGrid, PolarAngleAxis, PolarRadiusAxis, ResponsiveContainer, Legend, Tooltip
} from "recharts";
import type { SprintPlan } from "../types";

const COLORS = ["#38bdf8", "#22c55e", "#eab308", "#f472b6", "#a78bfa"];

/** Spider chart comparing plans across the 5 optimization criteria (F4.1). */
export default function RadarComparison({ plans }: { plans: SprintPlan[] }) {
  const maxBv = Math.max(1, ...plans.map((p) => p.metrics.totalBusinessValue));

  const axes = [
    { key: "Value", get: (p: SprintPlan) => p.metrics.totalBusinessValue / maxBv },
    { key: "Load balance", get: (p: SprintPlan) => p.metrics.loadBalanceScore },
    { key: "Skill fit", get: (p: SprintPlan) => p.metrics.averageSkillFit },
    { key: "Safety", get: (p: SprintPlan) => 1 - p.metrics.riskScore },
    { key: "Continuity", get: (p: SprintPlan) => p.metrics.continuityScore }
  ];

  const data = axes.map((axis) => {
    const row: Record<string, number | string> = { axis: axis.key };
    plans.forEach((p, i) => { row[`plan${i}`] = Number(axis.get(p).toFixed(3)); });
    return row;
  });

  return (
    <ResponsiveContainer width="100%" height={320}>
      <RadarChart data={data}>
        <PolarGrid stroke="#334155" />
        <PolarAngleAxis dataKey="axis" tick={{ fill: "#94a3b8", fontSize: 12 }} />
        <PolarRadiusAxis domain={[0, 1]} tick={{ fill: "#475569", fontSize: 10 }} />
        {plans.map((p, i) => (
          <Radar key={p.id} name={p.label} dataKey={`plan${i}`}
            stroke={COLORS[i % COLORS.length]} fill={COLORS[i % COLORS.length]} fillOpacity={0.15} />
        ))}
        <Legend />
        <Tooltip contentStyle={{ background: "#1e293b", border: "1px solid #334155" }} />
      </RadarChart>
    </ResponsiveContainer>
  );
}

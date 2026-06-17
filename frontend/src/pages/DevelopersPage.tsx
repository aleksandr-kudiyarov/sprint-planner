import { useState } from "react";
import { api } from "../api";
import { useAsync } from "../useAsync";
import type { CompetencyLevel } from "../types";

const LEVELS: CompetencyLevel[] = ["None", "Junior", "Mid", "Senior", "Expert"];

export default function DevelopersPage() {
  const devs = useAsync(() => api.listDevelopers());
  const comps = useAsync(() => api.listCompetencies());
  const stats = useAsync(() => api.developerStats());

  const [devForm, setDevForm] = useState({ name: "", email: "", capacityHoursPerDay: 6 });
  const [compForm, setCompForm] = useState({ name: "", description: "" });

  const statById = Object.fromEntries((stats.data ?? []).map((s) => [s.developerId, s]));

  async function addDev() {
    await api.createDeveloper(devForm);
    setDevForm({ name: "", email: "", capacityHoursPerDay: 6 });
    devs.reload();
  }
  async function addComp() {
    await api.createCompetency(compForm);
    setCompForm({ name: "", description: "" });
    comps.reload();
  }
  async function setLevel(devId: string, competencyId: string, level: string) {
    await api.setCompetency(devId, competencyId, level);
    devs.reload();
  }

  return (
    <div>
      <div className="panel">
        <h2>Add developer</h2>
        <div className="row">
          <div className="field"><label>Name</label>
            <input value={devForm.name} onChange={(e) => setDevForm({ ...devForm, name: e.target.value })} /></div>
          <div className="field"><label>Email</label>
            <input value={devForm.email} onChange={(e) => setDevForm({ ...devForm, email: e.target.value })} /></div>
          <div className="field"><label>Capacity (h/day)</label>
            <input type="number" value={devForm.capacityHoursPerDay}
              onChange={(e) => setDevForm({ ...devForm, capacityHoursPerDay: Number(e.target.value) })} /></div>
          <button onClick={addDev} disabled={!devForm.name}>Add</button>
        </div>
      </div>

      <div className="panel">
        <h2>Competencies</h2>
        <div className="row">
          <div className="field"><label>Name</label>
            <input value={compForm.name} onChange={(e) => setCompForm({ ...compForm, name: e.target.value })} /></div>
          <div className="field" style={{ flex: 1 }}><label>Description</label>
            <input value={compForm.description} onChange={(e) => setCompForm({ ...compForm, description: e.target.value })} /></div>
          <button onClick={addComp} disabled={!compForm.name}>Add</button>
        </div>
        <div style={{ marginTop: "0.5rem" }}>
          {(comps.data ?? []).map((c) => <span key={c.id} className="badge" style={{ marginRight: 6 }}>{c.name}</span>)}
        </div>
      </div>

      <div className="panel">
        <h2>Team & skill matrix</h2>
        <table>
          <thead>
            <tr>
              <th>Developer</th><th>Capacity</th><th>Velocity (β)</th>
              {(comps.data ?? []).map((c) => <th key={c.id}>{c.name}</th>)}
            </tr>
          </thead>
          <tbody>
            {(devs.data ?? []).map((d) => {
              const st = statById[d.id];
              return (
                <tr key={d.id}>
                  <td>{d.name}{!d.isActive && <span className="badge"> inactive</span>}</td>
                  <td className="muted">{d.capacityHoursPerDay} h/day</td>
                  <td>
                    {st && st.hasSufficientData
                      ? <span>{st.velocity?.toFixed(0)}h (β {st.beta.toFixed(2)})</span>
                      : <span className="badge warn">insufficient data</span>}
                  </td>
                  {(comps.data ?? []).map((c) => {
                    const owned = d.competencies.find((x) => x.competencyId === c.id);
                    return (
                      <td key={c.id}>
                        <select value={owned?.level ?? "None"} onChange={(e) => setLevel(d.id, c.id, e.target.value)}>
                          {LEVELS.map((l) => <option key={l} value={l}>{l}</option>)}
                        </select>
                      </td>
                    );
                  })}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

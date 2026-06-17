import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api";
import { useAsync } from "../useAsync";

export default function SprintsPage() {
  const { data: sprints, error, reload } = useAsync(() => api.listSprints());
  const navigate = useNavigate();
  const [form, setForm] = useState({ name: "", startDate: "", endDate: "", goal: "" });
  const [formError, setFormError] = useState<string | null>(null);

  async function create() {
    try {
      setFormError(null);
      await api.createSprint(form);
      setForm({ name: "", startDate: "", endDate: "", goal: "" });
      reload();
    } catch (e) {
      setFormError((e as Error).message);
    }
  }

  return (
    <div>
      <div className="panel">
        <h2>New sprint</h2>
        <div className="row">
          <div className="field">
            <label>Name</label>
            <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          </div>
          <div className="field">
            <label>Start</label>
            <input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          </div>
          <div className="field">
            <label>End</label>
            <input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} />
          </div>
          <div className="field" style={{ flex: 1 }}>
            <label>Goal</label>
            <input value={form.goal} onChange={(e) => setForm({ ...form, goal: e.target.value })} />
          </div>
          <button onClick={create} disabled={!form.name || !form.startDate || !form.endDate}>Create</button>
        </div>
        {formError && <p className="error">{formError}</p>}
      </div>

      <div className="panel">
        <h2>Sprints</h2>
        {error && <p className="error">{error}</p>}
        <table>
          <thead>
            <tr><th>Name</th><th>Dates</th><th>Status</th><th>Goal</th><th></th></tr>
          </thead>
          <tbody>
            {(sprints ?? []).map((s) => (
              <tr key={s.id}>
                <td>{s.name}</td>
                <td className="muted">{s.startDate} → {s.endDate}</td>
                <td><span className="badge">{s.status}</span></td>
                <td className="muted">{s.goal}</td>
                <td><button className="secondary" onClick={() => navigate(`/sprints/${s.id}/plan`)}>Plan</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

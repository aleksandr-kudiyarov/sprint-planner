import { useState } from "react";
import { api } from "../api";
import { useAsync } from "../useAsync";
import type { BacklogItemType } from "../types";

const TYPES: BacklogItemType[] = ["Feature", "Bug", "TechDebt", "Review", "Chore"];

export default function BacklogPage() {
  const items = useAsync(() => api.listBacklog());
  const devs = useAsync(() => api.listDevelopers());
  const [form, setForm] = useState({
    title: "", type: "Feature" as BacklogItemType, priority: 1,
    businessValue: 5, estimatedHours: 8, externalId: ""
  });
  const [error, setError] = useState<string | null>(null);

  const devName = (id?: string | null) => devs.data?.find((d) => d.id === id)?.name ?? "—";

  async function add() {
    try {
      setError(null);
      await api.createBacklogItem({
        ...form,
        description: "",
        status: "ReadyForPlanning",
        externalId: form.externalId || null
      });
      setForm({ ...form, title: "", externalId: "" });
      items.reload();
    } catch (e) {
      setError((e as Error).message);
    }
  }

  async function toggleLock(id: string, current: string | null | undefined, devId: string) {
    const res = await api.setLock(id, current ? null : devId);
    if (res.warning) alert(res.warning);
    items.reload();
  }

  return (
    <div>
      <div className="panel">
        <h2>Add backlog item</h2>
        <div className="row">
          <div className="field" style={{ flex: 2 }}><label>Title</label>
            <input value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></div>
          <div className="field"><label>Type</label>
            <select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as BacklogItemType })}>
              {TYPES.map((t) => <option key={t}>{t}</option>)}
            </select></div>
          <div className="field"><label>Priority</label>
            <input type="number" value={form.priority} onChange={(e) => setForm({ ...form, priority: Number(e.target.value) })} /></div>
          <div className="field"><label>Business value</label>
            <input type="number" value={form.businessValue} onChange={(e) => setForm({ ...form, businessValue: Number(e.target.value) })} /></div>
          <div className="field"><label>Estimate (h)</label>
            <input type="number" value={form.estimatedHours} onChange={(e) => setForm({ ...form, estimatedHours: Number(e.target.value) })} /></div>
          <div className="field"><label>YouTrack id</label>
            <input value={form.externalId} onChange={(e) => setForm({ ...form, externalId: e.target.value })} /></div>
          <button onClick={add} disabled={!form.title || form.estimatedHours <= 0}>Add</button>
        </div>
        {error && <p className="error">{error}</p>}
      </div>

      <div className="panel">
        <h2>Backlog</h2>
        <table>
          <thead>
            <tr><th>Title</th><th>Type</th><th>Prio</th><th>BV</th><th>Est</th><th>Status</th><th>Locked to</th></tr>
          </thead>
          <tbody>
            {(items.data ?? []).map((b) => (
              <tr key={b.id}>
                <td>{b.title}{b.externalId && <span className="muted"> ({b.externalId})</span>}</td>
                <td><span className="badge">{b.type}</span></td>
                <td>{b.priority}</td>
                <td>{b.businessValue}</td>
                <td>{b.estimatedHours}h</td>
                <td><span className="badge">{b.status}</span></td>
                <td>
                  <select
                    value={b.lockedDeveloperId ?? ""}
                    onChange={(e) => toggleLock(b.id, b.lockedDeveloperId, e.target.value)}
                  >
                    <option value="">🔓 none</option>
                    {(devs.data ?? []).map((d) => <option key={d.id} value={d.id}>🔒 {d.name}</option>)}
                  </select>
                  {b.lockedDeveloperId && <span className="muted"> {devName(b.lockedDeveloperId)}</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

import { NavLink, Outlet } from "react-router-dom";

export default function App() {
  return (
    <div className="app">
      <header className="topbar">
        <h1>Sprint Planner</h1>
        <nav>
          <NavLink to="/">Sprints</NavLink>
          <NavLink to="/developers">Team</NavLink>
          <NavLink to="/backlog">Backlog</NavLink>
        </nav>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}

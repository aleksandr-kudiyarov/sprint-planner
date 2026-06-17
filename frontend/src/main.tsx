import React from "react";
import ReactDOM from "react-dom/client";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import App from "./App";
import DevelopersPage from "./pages/DevelopersPage";
import BacklogPage from "./pages/BacklogPage";
import SprintsPage from "./pages/SprintsPage";
import PlanningPage from "./pages/PlanningPage";
import "./styles.css";

const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      { index: true, element: <SprintsPage /> },
      { path: "developers", element: <DevelopersPage /> },
      { path: "backlog", element: <BacklogPage /> },
      { path: "sprints/:sprintId/plan", element: <PlanningPage /> }
    ]
  }
]);

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>
);

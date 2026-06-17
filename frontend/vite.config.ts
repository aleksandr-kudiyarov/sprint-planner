import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// During development, proxy /api and /healthz to the backend so the SPA and API
// share an origin (no CORS juggling locally).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": { target: "http://localhost:5080", changeOrigin: true },
      "/healthz": { target: "http://localhost:5080", changeOrigin: true }
    }
  }
});

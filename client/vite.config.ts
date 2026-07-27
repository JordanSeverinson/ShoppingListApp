import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

function buildCspConnectSrc(apiUrl: string | undefined): string {
  // Same-origin only by default (Vite proxy in dev). Add explicit API + WS origins when set.
  const sources = new Set<string>(["'self'"]);

  if (apiUrl) {
    try {
      const url = new globalThis.URL(apiUrl);
      sources.add(url.origin);
      const wsOrigin = url.origin.replace(/^http/, "ws");
      sources.add(wsOrigin);
    } catch {
      // ignore invalid VITE_API_URL
    }
  } else {
    // Dev proxy: browser talks to Vite origin for /api and /hubs (ws).
    sources.add("ws://localhost:5173");
    sources.add("wss://localhost:5173");
  }

  return [...sources].join(" ");
}

const cspConnectSrc = buildCspConnectSrc(process.env.VITE_API_URL);

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    {
      name: "inject-csp-connect-src",
      transformIndexHtml(html) {
        return html.replace("%CSP_CONNECT_SRC%", cspConnectSrc);
      },
    },
  ],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5294",
        changeOrigin: true,
      },
      "/hubs": {
        target: "http://localhost:5294",
        changeOrigin: true,
        ws: true,
      },
    },
  },
});

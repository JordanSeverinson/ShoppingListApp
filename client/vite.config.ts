import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

function buildCspConnectSrc(apiUrl: string | undefined): string {
  const base = "'self' ws: wss:";
  if (!apiUrl) {
    return base;
  }

  try {
    const origin = new globalThis.URL(apiUrl).origin;
    return `${base} ${origin}`;
  } catch {
    return base;
  }
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

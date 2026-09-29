import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import tailwindcss from "@tailwindcss/vite";
import mkcert from "vite-plugin-mkcert";
import { fileURLToPath } from "node:url";

/** Holds the build's version: CI passes `APP_VERSION`; a local build is `dev`. */
const version = process.env.APP_VERSION || "dev";

// The SPA is served from the .NET host's wwwroot in production (base '/', same-origin "/api/...").
// In dev, Vite runs over HTTPS (vite-plugin-mkcert → a locally-trusted cert, so the Secure auth
// cookie is kept and the OAuth redirect has no cert interstitial) and proxies "/api" to the backend's
// HTTPS profile. changeOrigin:false keeps Host=localhost:5174 so the OAuth redirect_uri + the session
// cookie stay on the dev origin (5174). secure:false accepts the .NET dev cert.
export default defineConfig(({ command }) => ({
  base: "/",
  define: { __APP_VERSION__: JSON.stringify(version) },
  plugins: [vue(), tailwindcss(), ...(command === "serve" ? [mkcert()] : [])],
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
  server: {
    port: 5174,
    proxy: {
      "/api": {
        target: process.env.WHEELHOUSE_API_PROXY ?? "https://localhost:8210",
        changeOrigin: false,
        secure: false,
      },
    },
  },
}));

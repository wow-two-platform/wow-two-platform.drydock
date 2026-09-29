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
// `--mode http` or VITE_HTTPS=false serves plain HTTP for headless previews; GitHub sign-in needs the HTTPS origin.
export default defineConfig(({ command, mode }) => {
  const https = mode !== "http" && process.env.VITE_HTTPS !== "false";

  return {
    base: "/",
    define: { __APP_VERSION__: JSON.stringify(version) },
    plugins: [vue(), tailwindcss(), ...(command === "serve" && https ? [mkcert()] : [])],
    resolve: {
      alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
    },
    build: {
      outDir: "dist",
      emptyOutDir: true,
    },
    server: {
      port: Number(process.env.VITE_PORT ?? 5174),
      strictPort: true,
      proxy: {
        "/api": {
          target: process.env.WHEELHOUSE_API_PROXY ?? "https://localhost:8210",
          changeOrigin: false,
          secure: false,
        },
      },
    },
  };
});

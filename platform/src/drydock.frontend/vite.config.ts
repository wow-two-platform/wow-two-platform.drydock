import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The SPA is served from the .NET host's wwwroot in production, so all asset paths
// are root-relative (base '/') and API calls use same-origin "/api/..." URLs.
// In dev, Vite proxies "/api" to the backend's HTTP launch profile (port 8211).
export default defineConfig({
  base: '/',
  plugins: [react(), tailwindcss()],
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  },
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:8211',
        changeOrigin: true,
      },
    },
  },
});

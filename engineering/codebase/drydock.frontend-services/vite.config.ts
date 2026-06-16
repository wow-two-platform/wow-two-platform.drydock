import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import mkcert from 'vite-plugin-mkcert';

// The SPA is served from the .NET host's wwwroot in production (base '/', same-origin "/api/...").
// In dev, Vite runs over HTTPS (vite-plugin-mkcert → a locally-trusted cert, so the Secure auth
// cookie is kept and the OAuth redirect has no cert interstitial) and proxies "/api" to the backend's
// HTTPS profile. changeOrigin:false keeps Host=localhost:5174 so the OAuth redirect_uri + the session
// cookie stay on the dev origin (5174). secure:false accepts the .NET dev cert.
export default defineConfig({
  base: '/',
  plugins: [react(), tailwindcss(), mkcert()],
  build: {
    outDir: 'dist',
    emptyOutDir: true,
  },
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'https://localhost:8210',
        changeOrigin: false,
        secure: false,
      },
    },
  },
});

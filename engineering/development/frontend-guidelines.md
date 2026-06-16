# Drydock — Frontend Development Guidelines

*Last updated: 2026-06-12*

> Shared frontend conventions are a future sibling of `wow-two-ws/conventions/` (planned `conventions/frontend/`). Until it lands, follow the points below + `@wow-two-beta/ui` usage patterns. Document repo-specific deltas here.

## Stack

- React 19 · Vite 6 · TypeScript (strict) · Tailwind v4.
- UI: **`@wow-two-beta/ui`** — prefer its components (Button, Card, Badge, Heading, Text, EmptyState, Alert, Spinner, TextInput, …) before hand-rolling. Tailwind v4 wiring: `index.css` imports `tailwindcss` + `@wow-two-beta/ui/styles.css` and `@source`s the package's `dist` so its utility classes generate. Missing component → build it locally, then migrate it upstream.

## Conventions

- Single Vite app at `engineering/codebase/drydock.frontend-services/`. API client is same-origin (`/api/...`); dev proxies to the backend on `:8211` (see `vite.config.ts`). Dashboard on `:5174`.
- Production: `npm run deploy` (`scripts/deploy.mjs`) builds the SPA and copies `dist/` into the API's `wwwroot` (single-host serving). Idempotent — `wwwroot` is wiped + repopulated each run.

## Repo-specific deltas

- Control-plane dashboard only — single operator, never public. No client-side caching of secret values beyond the immediate view.

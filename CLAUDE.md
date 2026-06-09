# wow-two-platform.drydock

## What is this

**Drydock** — the internal product ops & deploy control plane for the micro-SaaS portfolio. Deploys
product front+back to Hetzner VPSs over SSH (Docker + Traefik), buys + wires domains (Porkbun +
Cloudflare), holds secrets, and watches the fleet. Single-user; **never exposed publicly**.

> This is a **platform service** in the `wow-two-platform` org. It manages the *other* products —
> it sits above them operationally.

## Structure

```
business/                 ← business logic & planning (not code) — roadmap, flows
platform/src/
├── drydock.backend/      ← .NET 10 Clean Architecture solution (Drydock.slnx)
└── drydock.frontend/     ← React 19 + Vite + Tailwind v4 + @wow-two-beta/ui
```

Backend layers: `Domain` (entities/enums/Result) → `Application` (MediatR CQRS + store abstractions)
→ `Infrastructure` (adapters) + `Persistence` (EF Core SQLite) → `Api` (slim host). Mirrors the
`wow-two-platform.secrets-vault` sibling exactly.

## Core domains (the 5 things Drydock manages)

Products · Servers · Deployments · Domains · Secrets. v1 wires **Servers** end-to-end (register +
list); the others exist as Domain entities + DbSets and get their Application/Api verticals next.

## Build & run

```bash
# Backend
cd platform/src/drydock.backend && dotnet build Drydock.slnx
dotnet run --project Drydock.Api --launch-profile https   # 8210 https / 8211 http

# EF migrations (local tool pinned in .config/dotnet-tools.json)
dotnet tool restore
dotnet ef migrations add <Name> --project Drydock.Persistence --startup-project Drydock.Api

# Frontend
cd platform/src/drydock.frontend && npm install && npm run dev   # 5174, proxies /api → 8211
npm run deploy   # build + copy SPA into Drydock.Api/wwwroot
```

## Conventions

- **File-per-type**, slim `Program.cs` (delegates to `Api/Configurations/*`), Result pattern in
  `Domain/Results`, `ResultError` → HTTP status via `ApiResults`. Controllers send MediatR requests
  via `ISender` and `Match` the `Result`.
- **Ports:** HTTPS even (8210) + HTTP odd (8211), per the wow-two launch-profile rule.
- **DB:** SQLite (`drydock.db`), one file, trivial to back up. Swap the provider in
  `Persistence/DependencyInjection.cs` for Postgres if/when multi-instance is needed.

## Beta SDK usage (per workspace direction)

- **Frontend → `@wow-two-beta/ui`.** Use its components (Button, Card, Badge, Heading, Text,
  EmptyState, Alert, Spinner, TextInput, …) before hand-rolling. Tailwind v4 wiring: `index.css`
  imports `tailwindcss` + `@wow-two-beta/ui/styles.css` and `@source`s the package's `dist` so its
  utility classes are generated. If a component is missing, build it locally, then migrate it upstream.
- **Backend → `WoW.Two.Sdk.Backend.Beta` (migration target, not yet referenced).** v1 uses raw
  MediatR + EF Core (the proven secrets-vault pattern) so the scaffold builds cleanly today. The beta
  backend is on nuget.org but still maturing (some modules compile-excluded; bespoke mediator surface).
  **Plan:** once a restore-verified spike confirms its hosting/observability/problemdetails/mediator
  helpers, migrate `Api/Configurations` + the pipeline onto it. Build-locally-then-migrate is the rule.

## Security

Drydock will hold VPS SSH keys, registrar billing APIs, the Cloudflare token, and a GHCR PAT — the
highest-value secret set in the portfolio. Keep it off the public internet (Tailscale / SSH tunnel),
encrypt secrets at rest, use scoped tokens, and keep an audit trail.

## Out of scope (deliberately, for now)

Auth/multi-tenant/billing (single-user — bind to Tailscale). No tests yet in the scaffold; add them
alongside the SSH deploy executor (the first logic worth locking down).

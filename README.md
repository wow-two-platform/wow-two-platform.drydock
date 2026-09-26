# wow-two-platform.wheelhouse

**Wheelhouse** — the product ops & deploy control plane for the micro-SaaS portfolio. The essential slice deploys published
product artifacts to VPSs defined in code. Domain, cost, backup and fleet governance follow that pilot.
Named DryDock until 2026-09-26 — a ship moving containers, where Docker is the whale that carries them.

> Internal tooling — **never expose publicly**. Bind to loopback and reach it over Tailscale / an SSH tunnel.

## Layout

```
product/                          ← the definition (what · why · planning) — no code
└── product.md · context.md · features/ · flows/ · planning/
engineering/                      ← the execution (build · ship · run)
├── engineering.md · architecture/ · development/ · deployment/ · planning/ (incl. version-track/) · research/ · scripts/
└── codebase/
    ├── wheelhouse.backend-services/         ← .NET 10 API (Clean Architecture + MediatR + EF Core/PostgreSQL)
    │   ├── Wheelhouse.Domain        ← entities, enums, Result pattern
    │   ├── Wheelhouse.Application   ← CQRS commands/queries (MediatR), store abstractions
    │   ├── Wheelhouse.Infrastructure← integration clients and bounded deployment gateway
    │   ├── Wheelhouse.Persistence   ← EF Core PostgreSQL context, stores, migrations
    │   └── Wheelhouse.Api           ← slim host, controllers, serves the SPA from wwwroot
    └── wheelhouse.frontend-services/        ← React 19 + Vite + Tailwind v4 + @wow-two-beta/ui dashboard
```

Follows `wow-two-ws/conventions/development/repo/structure/repo-structure.md`.

## Run it (dev)

**Backend** (API on `https://localhost:8210` / `http://localhost:8211`):
```bash
cd engineering/codebase/wheelhouse.backend-services
dotnet run --project Wheelhouse.Api --launch-profile https
```

**Frontend** (Vite on `http://localhost:5174`, proxies `/api` → `:8211`):
```bash
cd engineering/codebase/wheelhouse.frontend-services
npm install
npm run dev
```

Open http://localhost:5174 — the dashboard hits the API through the dev proxy.

## Single-host build (API serves the SPA)

```bash
cd engineering/codebase/wheelhouse.frontend-services && npm run deploy   # build SPA → copy into Api/wwwroot
cd ../wheelhouse.backend-services && dotnet run --project Wheelhouse.Api
```

Container startup requires an explicit database password and owner login; see
[deployment operations](engineering/deployment/deployment.md#local-packaging).

## Stack

- **Backend:** .NET 10, Clean Architecture, CQRS (MediatR 12), EF Core 10 + PostgreSQL, slim `Program.cs`.
- **Frontend:** React 19, Vite 6, Tailwind v4, `@wow-two-beta/ui` component library.
- **Runtime (target):** Docker + Traefik per Hetzner VPS; images from GHCR.

The full design spec lives in the workspace at `wow-two-ws/ideas/wheelhouse-spec.md`. The build plan is in
[`product/planning/planning.md`](product/planning/planning.md).

Fleet definitions and release-source integrations are code-owned; there is no Add VPS UI.
The [CI/artifact policy](engineering/planning/ci-artifact-policy.md) defines GitHub builds, release cuts and registry retention.

# wow-two-platform.drydock

**Drydock** — the product ops & deploy control plane for the micro-SaaS portfolio. One dashboard
to deploy product front+back to Hetzner VPSs over SSH, buy + wire domains, and keep the fleet alive.

> Internal tooling — **never expose publicly**. Bind to loopback and reach it over Tailscale / an SSH tunnel.

## Layout

```
business/                         ← business logic & planning (roadmap, flows)
platform/                         ← the deployable platform
├── Dockerfile · docker-compose.yml
└── src/
    ├── drydock.backend/          ← .NET 10 API (Clean Architecture + MediatR + EF Core/SQLite)
    │   ├── Drydock.Domain        ← entities, enums, Result pattern
    │   ├── Drydock.Application   ← CQRS commands/queries (MediatR), store abstractions
    │   ├── Drydock.Infrastructure← clock; (next) SSH / Hetzner / Porkbun / Cloudflare adapters
    │   ├── Drydock.Persistence   ← EF Core SQLite context, stores, migrations
    │   └── Drydock.Api           ← slim host, controllers, serves the SPA from wwwroot
    └── drydock.frontend/         ← React 19 + Vite + Tailwind v4 + @wow-two-beta/ui dashboard
```

## Run it (dev)

**Backend** (API on `https://localhost:8210` / `http://localhost:8211`):
```bash
cd platform/src/drydock.backend
dotnet run --project Drydock.Api --launch-profile https
```

**Frontend** (Vite on `http://localhost:5174`, proxies `/api` → `:8211`):
```bash
cd platform/src/drydock.frontend
npm install
npm run dev
```

Open http://localhost:5174 — the dashboard hits the API through the dev proxy.

## Single-host build (API serves the SPA)

```bash
cd platform/src/drydock.frontend && npm run deploy   # build SPA → copy into Api/wwwroot
cd ../drydock.backend && dotnet run --project Drydock.Api
```

Or the container (API + SPA in one image):
```bash
cd platform && docker build -t drydock . && docker compose up -d
```

## Stack

- **Backend:** .NET 10, Clean Architecture, CQRS (MediatR 12), EF Core 10 + SQLite, slim `Program.cs`.
- **Frontend:** React 19, Vite 6, Tailwind v4, `@wow-two-beta/ui` component library.
- **Runtime (target):** Docker + Traefik per Hetzner VPS; images from GHCR.

The full design spec lives in the workspace at `wow-two-ws/ideas/drydock-spec.md`. The build plan is in
[`business/planning/drydock-roadmap.md`](business/planning/drydock-roadmap.md).

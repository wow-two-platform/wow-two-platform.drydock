# wow-two-platform.drydock

## What is this

**Drydock** — the internal product ops & deploy control plane for the micro-SaaS portfolio. Deploys
product front+back to Hetzner VPSs over SSH (Docker + Traefik), buys + wires domains (Porkbun +
Cloudflare), holds secrets, and watches the fleet. Single-user; **never exposed publicly**.

> This is a **platform service** in the `wow-two-platform` org. It manages the *other* products —
> it sits above them operationally.

## Structure

```
product/                  ← the definition (what · why · features · flows) — no code
└── product.md · context.md · features/ · flows/ · planning/
engineering/              ← the execution (build · ship · run)
├── engineering.md · architecture/ · development/ · deployment/ · planning/ · versions/ · research/ · scripts/
└── codebase/
    ├── drydock.backend-services/   ← .NET 10 Clean Architecture solution (Drydock.slnx)
    └── drydock.frontend-services/  ← React 19 + Vite + Tailwind v4 + @wow-two-beta/ui
```

Follows `wow-two-ws/conventions/development/repo/repo-structure.md`.

Backend layers: `Domain` (entities/enums/Result) → `Application` (MediatR CQRS + store abstractions)
→ `Infrastructure` (adapters) + `Persistence` (EF Core + Postgres) → `Api` (slim host). Mirrors the
`wow-two-platform.secrets-vault` sibling exactly.

## Core domains (the 5 things Drydock manages)

Products · Servers · Deployments · Domains · Secrets. **Products** (create/list/update/delete +
version-resolution) and **Servers** (register/list/delete) are wired end-to-end; Deployments/Domains/Secrets
exist as Domain entities + DbSets, verticals next.

## Build & run

```bash
# Backend
cd engineering/codebase/drydock.backend-services && dotnet build Drydock.slnx
dotnet run --project Drydock.Api --launch-profile https   # 8210 https / 8211 http

# DB migrations: hand-authored SQL (bespoke migrator, applied on boot). No EF tooling.
# Add one → Drydock.Persistence/Migrations/{NNN-name}/{Apply,Rollback}.sql

# Frontend
cd engineering/codebase/drydock.frontend-services && npm install && npm run dev   # 5174, proxies /api → 8211
npm run deploy   # build + copy SPA into Drydock.Api/wwwroot
```

## Testing

3-tier, e2e-first (run all: `dotnet test Drydock.slnx`). Solution folders: `services/` + `tests/`.

- **`Drydock.Tests`** — unit (pure logic: version-state machine, validators). Docker-free.
- **`Drydock.Migrations.Tests`** — integration (migrator + persistence vs real PG, no HTTP). Docker.
- **`Drydock.IntegrationTests`** — e2e (full host + Testcontainers PG, on `…Beta.Testing`). Docker.

Reserve unit for I/O-free logic; everything user-facing is covered e2e. Full rule:
`wow-two-ws/conventions/development/backend/testing/testing.md`.

## Conventions

- **File-per-type**, slim `Program.cs` (delegates to `Api/Configurations/*`), Result pattern in
  `Domain/Results`, `ResultError` → HTTP status via `ApiResults`. Controllers send MediatR requests
  via `ISender` and `Match` the `Result`.
- **Ports:** HTTPS even (8210) + HTTP odd (8211), per the wow-two launch-profile rule.
- **DB:** Postgres (Npgsql). Schema owned by the **bespoke SQL migrator**
  (`…Beta.Data.Migrations.Bespoke`) over hand-authored `Migrations/{NNN}/{Apply,Rollback}.sql`;
  EF Core is a pure mapper. Migrates on boot; Testcontainers-PG under E2E.

## Beta SDK usage (per workspace direction)

- **Frontend → `@wow-two-beta/ui`.** Use its components (Button, Card, Badge, Heading, Text,
  EmptyState, Alert, Spinner, TextInput, …) before hand-rolling. Tailwind v4 wiring: `index.css`
  imports `tailwindcss` + `@wow-two-beta/ui/styles.css` and `@source`s the package's `dist` so its
  utility classes are generated. If a component is missing, build it locally, then migrate it upstream.
- **Backend → `WoW.Two.Sdk.Backend.Beta` (adopted, `10.0.34-beta`).** `v0.2` migrated every layer onto
  the SDK: host floor (`AddApiDefaults`/`UseApiDefaults`), mediator + results + validation, identity
  (GitHub OAuth + cookie + allowlist/default-deny), `Integrations.GitHub`/`Ghcr` clients, the bespoke SQL
  migrator, and `…Beta.Testing` for the test harness. Products hold business logic only; new infra proves
  inline then extracts to the SDK in the next `+0.1` (see `engineering/planning/backlog.md`).

## Security

Drydock will hold VPS SSH keys, registrar billing APIs, the Cloudflare token, and a GHCR PAT — the
highest-value secret set in the portfolio. Keep it off the public internet (Tailscale / SSH tunnel),
encrypt secrets at rest, use scoped tokens, and keep an audit trail.

## Out of scope (deliberately, for now)

Auth/multi-tenant/billing (single-user — bind to Tailscale).

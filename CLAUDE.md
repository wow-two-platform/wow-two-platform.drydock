# wow-two-platform.wheelhouse

## What is this

**Wheelhouse** — the internal product ops & deploy control plane for the micro-SaaS portfolio. Deploys
product release bundles to code-owned VPS targets over SSH (Docker + ingress). Domain governance,
secret rotation and fleet monitoring remain planned capabilities. Single-user; **never exposed publicly**.
Named DryDock until 2026-09-26; history before then says DryDock.

> This is a **platform service** in the `wow-two-platform` org. It manages the *other* products —
> it sits above them operationally.

## Structure

```
product/                  ← the definition (what · why · features · flows) — no code
└── product.md · context.md · features/ · flows/ · planning/
engineering/              ← the execution (build · ship · run)
├── engineering.md · architecture/ · development/ · deployment/ · planning/ (incl. version-track/) · research/ · scripts/
└── codebase/
    ├── wheelhouse.backend-services/   ← .NET 10 Clean Architecture solution (Wheelhouse.BackendServices.slnx)
    ├── wheelhouse.frontend-services/  ← Vue 3 + Vite + Tailwind v4 + @wow-two-beta/ui-vue (layered, SDK query layer)
    └── wheelhouse.runner-services/    ← Python operator runner: fleet, catalog, SSH transport, target executor
```

Follows `wow-two-ws/conventions/development/repo/structure/repo-structure.md`.

Backend layers: `Domain` (entities/enums/Result) → `Application` (MediatR CQRS + repository abstractions)
→ `Infrastructure` (adapters) + `Persistence` (EF Core + Postgres) → `Api` (slim host). Mirrors the
`wow-two-platform.secrets-vault` sibling exactly.

## Core domains (the 5 things Wheelhouse manages)

Products · Servers · Deployments · Domains · Secrets. **Products** (create/list/update/delete) and **Servers**
(read-only code-owned fleet) are wired end-to-end. Deployments use the independent runner and published artifact
catalog, with history, read-only checks and reconciliation. **Secrets** are administered in code-owned vaults through
the vault console (write-only values). Domains remain a scaffold model.

## Build & run

```bash
# Backend
cd engineering/codebase/wheelhouse.backend-services && dotnet build Wheelhouse.BackendServices.slnx
dotnet run --project Wheelhouse.Api --launch-profile https   # 8210 https / 8211 http

# DB migrations: hand-authored SQL (bespoke migrator, applied on boot). No EF tooling.
# Add one → Wheelhouse.Persistence/Migrations/{NNN-name}/{Apply,Rollback}.sql

# Frontend
cd engineering/codebase/wheelhouse.frontend-services && npm install && npm run dev   # HTTPS 5174, proxies /api → HTTPS 8210; Node 22+
npm run test    # inventory, selection, protocol and sensitive-form lifecycle checks
npm run build   # vue-tsc + SFC compiler gate + route-split production bundle
npm run deploy   # build + copy SPA into Wheelhouse.Api/wwwroot

# Local rig for an IDE run (then Wheelhouse.Api, profile https → https://localhost:8210)
cd engineering/deployment/rehearsal && python3 rehearse.py dev
# The whole system locally: production image + rehearsal SSH target + vault (deployment.md → Local console)
cd engineering/deployment/rehearsal && python3 rehearse.py console   # http://localhost:18210
```

## Testing

4-tier `{Product}.Tests.{Type}`, e2e-first (run all: `dotnet test Wheelhouse.BackendServices.slnx`). Solution folders: `services/` + `tests/`.

- **`Wheelhouse.Tests.Unit`** — pure logic (version-state machine, validators). Docker-free.
- **`Wheelhouse.Tests.Integration`** — the EF model below the pipeline: `WheelhouseDbContext` over the SDK
  `RelationalTestDb` (enum round-trip, repository predicates/ordering, unique-index constraints), no HTTP.
  Provider-switchable PG↔SQLite (`WHEELHOUSE_TEST_DB=sqlite`). Docker (PG default).
- **`Wheelhouse.Tests.E2E`** — full host + Testcontainers PG (on `…Beta.Testing`). The primary tier. Docker.
- **`Wheelhouse.Tests.Migrations`** — specialized: the bespoke SQL migrator's apply/idempotency/rollback over a real
  PG, on the SDK `MigratorHarness` + `MigratorPostgresFixture` (embedded `Migrations/NNN/*.sql`). Docker.

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

- **Frontend → `@wow-two-beta/ui-vue` (`0.0.7`).** Use its components (Button, Card, Badge, Heading, Text,
  EmptyState, Alert, Spinner, TextInput, …) before hand-rolling. Tailwind v4 wiring: `index.css`
  imports `tailwindcss` + `@wow-two-beta/ui-vue/styles.css` and `@source`s the package's `dist` so its
  utility classes are generated. Shared capability gaps belong in the SDK. Product composition stays local.
- **Backend → `WoW.Two.Sdk.Backend.Beta` (adopted, `10.0.40-beta`).** `v0.2` migrated every layer onto
  the SDK: host floor (`AddApiDefaults`/`UseApiDefaults`), mediator + results + validation, identity
  (GitHub OAuth + cookie + allowlist/default-deny), `Integrations.GitHub`/`Ghcr` clients, the bespoke SQL
  migrator, and `…Beta.Testing` for the test harness. Products hold business logic only; new infra proves
  inline then extracts to the SDK in the next `+0.1` (see `engineering/planning/backlog.md`).

## Security

Wheelhouse will hold VPS SSH keys, registrar billing APIs, the Cloudflare token, and a GHCR PAT — the
highest-value secret set in the portfolio. Keep it off the public internet (Tailscale / SSH tunnel),
encrypt secrets at rest, use scoped tokens, and keep an audit trail.

## Out of scope (deliberately, for now)

Auth/multi-tenant/billing (single-user — bind to Tailscale).

## Fleet and artifact policy

Providers and individual VPS bindings are defined in `engineering/codebase/wheelhouse.runner-services/fleet.py`.
Provider/environment choices use enums; no dynamic provider plugins or Add VPS UI/API.
`artifacts.py` owns approved release sources. Wheelhouse consumes published bundles and never triggers builds.
The detailed Git/CI/registry policy lives in `engineering/planning/ci-artifact-policy.md`.

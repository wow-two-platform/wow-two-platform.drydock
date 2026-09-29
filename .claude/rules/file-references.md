# File References

> **Lookup table only.** Do NOT read files proactively — only when the current task requires them.
> **Tracks docs outside `engineering/codebase/`.** Source files (`.cs`/`.ts`/`.vue`) are navigated via
> `tree`/`find`/`grep`, never listed here.

| Question about | Read |
|---|---|
| What it is, positioning | `product/product.md` |
| Current state, decisions, essential milestone | `product/context.md` |
| Flows | `product/flows/flows.md` |
| Brand assets, logo usage and sizes | `product/brand/brand.md` |
| Features, deferred work, open decisions | `engineering/planning/backlog.md` |
| Per-version progress (newest folder = active version) | `engineering/planning/version-track/v{X.Y}/v{X.Y}.md` |
| Runtime, release contract, execution, artifacts, fleet, workspace | `engineering/architecture/architecture.md` |
| Backend dev guidelines | `engineering/development/backend-guidelines.md` |
| Frontend dev guidelines | `engineering/development/frontend-guidelines.md` |
| Operational rules | `engineering/development/rules.md` |
| Deploy / ops, local rig, VPS wiring checklist | `engineering/deployment/deployment.md` |
| Next versions, close-out items | `engineering/research/next-versions.md` |
| Feature completeness, decision Points, sweep | `engineering/research/feature-completeness.md` |
| Deployment topology, environments, sites, builds | `engineering/research/deployment-topology.md` |
| Parked UI directions (Claude boards) | `engineering/research/design-directions/design-directions.md` |
| Brand and app logo export scripts | `engineering/scripts/export-brand-logos.py` · `export-app-icons.py` |

## Source projects (`engineering/codebase/`)

> Individual files NOT listed — use `tree`/`find`/`grep`. Projects only.

### `codebase/wheelhouse.backend-services/` (.NET Clean Arch — `Wheelhouse.BackendServices.slnx`, folders `Services/` + `Tests/`)
| Project | What it is |
|---|---|
| `Wheelhouse.Api` | HTTP host — control-plane controllers; single-host SPA serving |
| `Wheelhouse.Application` | Use cases — mediator handlers, repository abstractions, DTOs |
| `Wheelhouse.Domain` | Entities (Server/Product/Deployment/ManagedDomain/SecretEntry, audit, operations) + enums |
| `Wheelhouse.Infrastructure` | Adapters — runner process gateway, vault admin client, settings |
| `Wheelhouse.Persistence` | EF Core + Postgres context, repositories, hand-authored SQL migrations |
| `Wheelhouse.Tests.Unit` | **Unit** tier — pure logic (version-state machine, validators); Docker-free |
| `Wheelhouse.Tests.Integration` | **Integration** tier — EF model below the pipeline over the SDK `RelationalTestDb`, no HTTP; PG↔SQLite |
| `Wheelhouse.Tests.E2E` | **E2E** tier — full host + Testcontainers PG (on `…Beta.Testing`) |
| `Wheelhouse.Tests.Migrations` | **Migrations** tier — bespoke SQL migrator apply/idempotency/rollback over real PG, on the SDK `MigratorHarness` |

### `codebase/wheelhouse.frontend-services/` (pnpm workspace)
| App | What it is |
|---|---|
| `apps/web` (`@wheelhouse/web`) | Control-plane workspace — deployments, servers, secrets, products, activity |

### `codebase/wheelhouse.runner-services/` (Python)
| File | What it is |
|---|---|
| `fleet.py` | Code-owned providers, servers, targets, vaults |
| `artifacts.py` | Approved release sources and catalog |
| `transport.py` | Operator CLI + SSH adapter used by the API |
| `runner.py` | Target-side executor: locks, health gates, recovery |

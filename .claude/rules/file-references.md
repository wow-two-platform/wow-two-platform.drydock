# File References

> **Lookup table only.** Do NOT read files proactively — only when the current task requires them.
> **Tracks docs outside `engineering/codebase/`.** Source files (`.cs`/`.tsx`) are navigated via
> `tree`/`find`/`grep`, never listed here.

| Question about | Read |
|---|---|
| What it is, positioning | `product/product.md` |
| Current state, decisions | `product/context.md` |
| Features | `product/features/features.md` |
| Flows | `product/flows/flows.md` |
| Product milestones / roadmap | `product/planning/planning.md` |
| System architecture, deploy/domain flows, data model | `engineering/architecture/architecture.md` |
| Backend dev guidelines | `engineering/development/backend-guidelines.md` |
| Frontend dev guidelines | `engineering/development/frontend-guidelines.md` |
| Iteration / version workflow | `engineering/development/iteration-guide.md` |
| Eng roadmap, component tracker, decisions | `engineering/planning/planning.md` |
| Backlog (deferred / known issues) | `engineering/planning/backlog.md` |
| Operational rules | `engineering/planning/rules.md` |
| Deploy / ops | `engineering/deployment/deployment.md` |
| Per-version progress | `engineering/versions/v{X.Y}/v{X.Y}.md` |

## Source projects (`engineering/codebase/`)

> Individual files NOT listed — use `tree`/`find`/`grep`. Projects only.

### `codebase/wheelhouse.backend-services/` (.NET Clean Arch — `Wheelhouse.BackendServices.slnx`, folders `services/` + `tests/`)
| Project | What it is |
|---|---|
| `Wheelhouse.Api` | HTTP host — control-plane controllers; single-host SPA serving |
| `Wheelhouse.Application` | Use cases — MediatR handlers, repository abstractions, DTOs |
| `Wheelhouse.Domain` | Entities (Server/Product/Deployment/ManagedDomain/SecretEntry) + enums + Result |
| `Wheelhouse.Infrastructure` | Adapters — clock now; SSH / Hetzner / Porkbun / Cloudflare / GHCR next |
| `Wheelhouse.Persistence` | EF Core + Postgres context, repositories, hand-authored SQL migrations |
| `Wheelhouse.Tests.Unit` | **Unit** tier — pure logic (version-state machine, validators); Docker-free |
| `Wheelhouse.Tests.Integration` | **Integration** tier — EF model below the pipeline (enum round-trip, repository predicates/ordering, constraints) over the SDK `RelationalTestDb`, no HTTP; PG↔SQLite |
| `Wheelhouse.Tests.E2E` | **E2E** tier — full host + Testcontainers PG (on `…Beta.Testing`) |
| `Wheelhouse.Tests.Migrations` | **Migrations** tier — bespoke SQL migrator apply/idempotency/rollback over real PG, on the SDK `MigratorHarness` |

### `codebase/wheelhouse.frontend-services/` (React)
| App | What it is |
|---|---|
| (root Vite app) | Control-plane dashboard — servers (+ products/deployments/domains/secrets next) |

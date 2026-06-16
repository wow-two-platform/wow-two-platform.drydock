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

### `codebase/drydock.backend-services/` (.NET Clean Arch — `Drydock.slnx`)
| Project | What it is |
|---|---|
| `Drydock.Api` | HTTP host — control-plane controllers; single-host SPA serving |
| `Drydock.Application` | Use cases — MediatR handlers, store abstractions, DTOs |
| `Drydock.Domain` | Entities (Server/Product/Deployment/ManagedDomain/SecretEntry) + enums + Result |
| `Drydock.Infrastructure` | Adapters — clock now; SSH / Hetzner / Porkbun / Cloudflare / GHCR next |
| `Drydock.Persistence` | EF Core + SQLite context, stores, migrations |

### `codebase/drydock.frontend-services/` (React)
| App | What it is |
|---|---|
| (root Vite app) | Control-plane dashboard — servers (+ products/deployments/domains/secrets next) |

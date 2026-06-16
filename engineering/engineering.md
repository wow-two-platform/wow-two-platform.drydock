# engineering/

The **how** of Drydock — all technical content.

| Folder | Purpose |
|---|---|
| `architecture/` | System design — control plane, deploy/domain flows, data model |
| `codebase/` | **The code** — `drydock.backend-services/` (.NET) + `drydock.frontend-services/` (React) |
| `development/` | Repo dev guidelines — defer to `wow-two-ws/conventions/`, document only deltas |
| `deployment/` | Dockerfile + compose (single-host image) |
| `planning/` | Eng roadmap + backlog (`planning.md`) + operational rules (`rules.md`) |
| `versions/` | Per-version progress docs (`v{X.Y}/v{X.Y}.md`) |
| `research/` | Technical spikes / comparisons |
| `scripts/` | Dev / ops scripts |

> Everything executable lives under `codebase/`. Never put services directly in `engineering/`.

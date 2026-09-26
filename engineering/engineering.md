# engineering/

The **how** of Wheelhouse — all technical content.

| Folder | Purpose |
|---|---|
| `architecture/` | System design — control plane, deploy/domain flows, data model |
| `codebase/` | **The code** — `wheelhouse.backend-services/` (.NET), `wheelhouse.frontend-services/` (React), `wheelhouse.runner-services/` (Python) |
| `development/` | Repo dev guidelines — defer to `wow-two-ws/conventions/`, document only deltas |
| `deployment/` | Dockerfile, compose, operations guide, local SSH target |
| `planning/` | Phases (`planning.md`), backlog, rules, pilot, CI policy, `version-track/v{X.Y}/` |
| `research/` | Technical spikes / comparisons |
| `scripts/` | Dev / ops scripts |

> Everything executable lives under `codebase/`. Never put services directly in `engineering/`.

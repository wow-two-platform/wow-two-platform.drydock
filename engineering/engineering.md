# engineering/

*Last updated: 2026-09-29*

The **how** of Wheelhouse — all technical content.

| Folder | Purpose |
|---|---|
| `architecture/` | System design — runtime, release contract, execution, artifacts, fleet, vaults, workspace |
| `codebase/` | **The code** — `wheelhouse.backend-services/` (.NET), `wheelhouse.frontend-services/` (Vue, pnpm workspace), `wheelhouse.runner-services/` (Python) |
| `development/` | Repo guidelines and operational rules — defer to `wow-two-ws/conventions/`, document only deltas |
| `deployment/` | Dockerfile, compose, `deploy.yml`, operations guide, local rehearsal rig |
| `planning/` | `backlog.md` and `version-track/v{X.Y}/v{X.Y}.md` — the newest folder is the active version |
| `research/` | Technical spikes, comparisons and parked design directions |
| `scripts/` | Dev and brand-export scripts |

> Everything executable lives under `codebase/`. Never put services directly in `engineering/`.

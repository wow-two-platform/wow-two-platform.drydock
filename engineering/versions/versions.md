# versions/

Per-version progress docs for Drydock. One folder per version, named exactly the version: `v{X.Y}/v{X.Y}.md` (no brand/platform prefix). The latest folder is the active version.

- **How to write one:** follow `wow-two-ws/conventions/planning/version-planning/version-docs.md`.
- **Shape:** iterations with one-line goals + abstract tasks — no schema/service/file detail.
- **Lifecycle:** `⏳ Planned` → `🚧 In Progress` → `✅ Complete` (+ completed date).
- **Cadence:** pre-MVP `+0.1` per major capability; post-MVP the bump reflects the change. **`v1.0` = MVP-complete** — the full deploy slice live (deploy + domains + live logs + history + rollback).

| Version | Theme | Status |
|---|---|---|
| `v0.1` | First deploy to a real box | ⏳ Planned |

> Only the next version is documented (convention). Deferred work / known issues live in `../planning/backlog.md`, not here. P0 (Clean-Arch skeleton + Servers vertical) shipped before version docs existed — it has no `v0.x` doc.

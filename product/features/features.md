# DryDock — Features

*Last updated: 2026-09-19*

| Feature | Current boundary | Later expansion |
|---|---|---|
| Product inventory | Create/list/update/delete portfolio metadata | Broader ownership and cost inventory |
| Fleet inventory | Read-only hosts and provider filters; definitions in code | Explicit provider integrations added in code |
| Deployment artifacts | Published bundle catalog from approved repositories | Signed provenance and broader registry sources |
| Deployment execution | Pinned SSH, serialized rollout, readiness, durable outcomes | Fleet-wide history and richer diagnostics |
| Recovery | Independent runner; previous images under a schema-compatibility gate | Automated restoration drills |
| Secrets | Protected mounted files and pinned identity references | Vault integration and rotation |
| Domains | Manual DNS/ingress wiring for the pilot | Registrar/DNS integrations and expiry tracking |
| Operations | Readiness and bounded deployment logs | External uptime, disk and backup-age monitoring |
| Data | Separate product/environment databases and backup runbook | Managed backup policies and verified restores |
| Costs/capacity | Manual host budget and headroom review | Provider billing and placement views |

Infrastructure governance is broader than deployment; the first essential slice must pass the
[pilot gates](../../engineering/planning/deployment-pilot.md) before the portfolio expands.
Dynamic provider plugins and UI-based VPS registration are excluded by product decision.

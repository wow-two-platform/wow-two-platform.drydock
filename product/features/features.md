# Wheelhouse — Features

*Last updated: 2026-09-26*

| Feature | Current boundary | Later expansion |
|---|---|---|
| Product inventory | Create/list/update/delete portfolio metadata | Broader ownership and cost inventory |
| Fleet inventory | Read-only hosts and provider filters; definitions in code | Explicit provider integrations added in code |
| Deployment artifacts | Published bundle catalog from approved repositories | Signed provenance and broader registry sources |
| Deployment execution | Pinned SSH, serialized rollout, readiness, durable outcomes, history, read-only checks | Richer diagnostics and live logs |
| Recovery | Independent runner; compatible image rollback; reconcile and redeploy from the dashboard | Automated restoration drills |
| Secrets | Vault console: namespaces, write-only secrets, product tokens, rotation hygiene; mounted runtime settings | Deploy-time token provisioning and expiring tokens |
| Domains | Manual DNS/ingress wiring for the pilot | Registrar/DNS integrations and expiry tracking |
| Operations | Host and container vitals, 30-day deployment metrics, release drift, one attention list | Vitals history, alerts, external uptime and backup-age monitoring |
| Data | Separate product/environment databases and backup runbook | Managed backup policies and verified restores |
| Costs/capacity | Manual host budget and headroom review | Provider billing and placement views |

Infrastructure governance is broader than deployment; the first essential slice must pass the
[pilot gates](../../engineering/planning/deployment-pilot.md) before the portfolio expands.
Dynamic provider plugins and UI-based VPS registration are excluded by product decision.

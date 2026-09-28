# Wheelhouse — Features

*Last updated: 2026-09-29*

| Feature | Current boundary | Later expansion |
|---|---|---|
| Product inventory | Create/list/update/delete portfolio metadata | One code-owned product catalog; ownership, cost and kill-gate metrics |
| Fleet inventory | Read-only hosts and provider filters; definitions in code | Host preparation from code; more providers added in code |
| Environments | `dev`, `test` and `prod` per product on one host; the local server runs all three | Branch and pull-request environments from code-owned templates |
| Deployment artifacts | A release catalog of published releases and per-commit builds from approved repositories; a build starts for a commit without one | Signed provenance and private release assets |
| Deployment execution | Pinned SSH, serialized rollout, health gates, prod only after a test pass, live rollout steps, durable outcomes | Deploy notifications and start/stop per environment |
| Sites | Products declare sites; targets name hosts; the ingress routes them; each site is requested after a deploy | Preview domain and DNS records from code |
| Recovery | Independent runner; compatible image rollback; reconcile and redeploy from the dashboard | Automated restoration drills |
| Service map | Per environment: services, networks, volumes, startup order, sites, platform needs and versions on a pan-and-zoom canvas; environments compared with one-click promotion | A host view with capacity and a portfolio matrix |
| Diagnostics | Read-only target check (SSH, Docker, disk, network, ingress, settings); a service's recent logs on request | Live log streaming |
| Audit | Every operator action in a hash-chained trail; the Activity page shows it and whether it verifies | An external checkpoint for the newest entries |
| Secrets | Vault console: namespaces, write-only secrets, product tokens, rotation hygiene; mounted runtime settings | Settings rendering, deploy-time tokens and expiring tokens |
| Domains | Site hosts per target, routed by the ingress | Registrar and DNS integrations, expiry tracking |
| Operations | Host and container vitals, 30 days of vitals trends, 30-day deployment metrics, release drift, one attention list; old images removed after each deploy | Alerts, external uptime and backup-age monitoring |
| Data | Separate product/environment databases and backup runbook | Platform PostgreSQL per host, managed backups and verified restores |
| Costs/capacity | Manual host budget and headroom review | Provider billing and placement views |
| Self-shipping | CI on every push; Wheelhouse builds its own image and bundle with its release generator | A private control host where Wheelhouse deploys itself |

Infrastructure governance is broader than deployment; the first essential slice must pass the
[pilot gates](../../engineering/planning/deployment-pilot.md) before the portfolio expands.
Dynamic provider plugins and UI-based VPS registration are excluded by product decision.
The [feature completeness analysis](../../engineering/research/feature-completeness.md) holds the open decisions.

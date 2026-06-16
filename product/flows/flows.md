# Drydock — Flows

*Last updated: 2026-06-12*

User / product flows. One doc per non-trivial flow; this is the index.

- **Deploy a product** — push to `main` → GitHub Actions builds web + api images → GHCR → operator clicks **Deploy** → Drydock queues a job (product × server × env) → render `docker-compose.yml` (image tags, Traefik labels, injected secrets) → SSH + `docker compose pull && up -d` → Traefik routes the domain + issues SSL → health check + live logs in the dashboard. **Rollback** = redeploy the previous SHA (one click).
- **Buy + wire a domain** — search (Porkbun) → buy against pre-funded balance → set nameservers to Cloudflare → create A-record → VPS IP → assign domain → product/env → next deploy writes the Traefik `Host()` rule → cert issued (~1–2 min DNS prop).
- **Register a server** — add VPS (host/IP, SSH key ref) → test SSH + Docker → it joins the fleet, available as a deploy target.
- **Teardown / kill** — stop the stack → final backup → archive repo → release/expire the domain (the micro-SaaS kill-gate, executed cleanly).

> Step-by-step deploy/domain detail + data model: `../../engineering/architecture/architecture.md`.

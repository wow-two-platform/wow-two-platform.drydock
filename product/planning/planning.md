# Drydock — Product Planning

*Last updated: 2026-06-12*

Product milestones + phasing (the roadmap). Engineering-side tracking lives in `../../engineering/planning/planning.md`.

First-ship principle: **one vertical slice, not horizontal layers.** Get one product deployed to a
real Hetzner box on a real domain, then widen.

## v1 milestone

> Click **Deploy** → a product's front+back lands on the Hetzner box, on a real domain with SSL,
> logs streaming live, one-click rollback.

## Phases

| Phase | Scope | Status |
|---|---|---|
| **P0 — Foundations** | Clean-Arch skeleton, slim host, EF SQLite, Servers registry (register + list), dashboard shell on `@wow-two-beta/ui` | ✅ scaffolded (this commit) |
| **P1 — Deploy (core)** | SSH executor (SSH.NET) → render compose → `docker compose pull && up -d`; live logs (SignalR); deploy history + rollback; Traefik + Let's Encrypt on the box; GHCR pull | ▶ next — spike the deploy thread first |
| **P2 — Domains** | Porkbun search/buy + Cloudflare A-record + assign → Traefik route | later |
| **P3 — Ops** | health/uptime + Telegram alerts, SSL/domain expiry, per-product DB + backups, cost-per-product | later |
| **P4 — Accelerate** | 0→live scaffold (template repo → GitHub API → CI → domain → deploy), Hetzner cloud-init one-click provisioning, teardown/kill-gates | later |

> P0 build status, the component tracker, the immediate deploy-thread spike, and open
> engineering decisions live in `../../engineering/planning/planning.md`.

# Drydock — Roadmap

*Last updated: 2026-06-09*

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

## Done in P0

- 5 domain modules modelled (`Server`, `Product`, `Deployment`, `ManagedDomain`, `SecretEntry`) + DbSets + `InitialCreate` migration.
- **Servers** vertical wired end-to-end: `RegisterServerCommand` / `ListServersQuery` → `ServersController` → `useServers` → `ServersPanel`. Verified: register, list, duplicate-host → 409.
- Backend builds clean (0 warnings); frontend typechecks + builds.

## Immediate next step (P1, step 1)

Spike the deploy thread on one hand-configured Hetzner box, hardcoded values:
**SSH (SSH.NET) → render `docker-compose.yml` → `docker compose pull && up -d` → stream stdout to
SignalR → Traefik routes host + issues Let's Encrypt cert → pull private image from GHCR.**
Prove it end-to-end, then productize into a Hangfire job + `Deployment` history.

## Open decisions

- Registrar: Porkbun (best API) vs Namecheap.
- Build vs wrap Coolify for the deploy substrate.
- Secrets: local AES table now → migrate to the `wow-two-platform.secrets-vault` service.
- Backend beta SDK adoption (see `CLAUDE.md` → Beta SDK usage).

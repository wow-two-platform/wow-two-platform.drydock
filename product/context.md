# Drydock — Context

*Last updated: 2026-06-12*

## Current state

P0 scaffolded. Clean-Arch .NET 10 backend (slim host, MediatR CQRS, EF Core + SQLite) + React 19 / Vite / Tailwind v4 dashboard on `@wow-two-beta/ui`, shipped as one single-host Docker image. Five domain modules modelled (`Server`, `Product`, `Deployment`, `ManagedDomain`, `SecretEntry`) + DbSets + `InitialCreate` migration. The **Servers** vertical is wired end-to-end (register + list; duplicate host → 409). Backend builds clean; frontend typechecks + builds. The repo has been restructured to the wow-two product-repo conventions (`product/` + `engineering/codebase/`).

## Active tasks

| Task | Status | Notes |
|---|---|---|
| Spike the deploy thread (SSH → compose → `docker compose up -d` → SignalR logs → Traefik + LE → GHCR pull) | todo | P1, step 1 — one hand-configured Hetzner box, hardcoded values |
| Build Products / Deployments / Domains / Secrets verticals | todo | Entities exist; add Application/Api |
| Authenticate the control plane | todo | Endpoints currently open |

## Open questions

- Registrar: Porkbun (best API) vs Namecheap (stricter API)?
- Build bespoke (Docker + Traefik + SSH.NET) vs wrap Coolify as the deploy substrate?
- One big VPS (bin-pack, Traefik shines) vs one-per-product (isolation)?
- How many Hetzner servers now — decides multi-server priority.

## Decisions

- 2026-06-09 — Name = **Drydock**; provider = **Hetzner Cloud** (full REST API + cloud-init).
- 2026-06-09 — Bespoke Docker + Traefik + SSH.NET + GHCR as the primary deploy substrate; Coolify as fast-lane fallback.
- 2026-06-12 — Conform to the repo-structure standard (`product/` + `engineering/codebase/{backend,frontend}-services`, folder-docs).

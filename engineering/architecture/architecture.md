# Drydock — Architecture

*Last updated: 2026-06-12*

## Overview

A single .NET 10 host serves the control-plane HTTP API plus (in production) the React dashboard from its `wwwroot`. Clean Architecture — dependencies point inward, the Domain has no infrastructure dependency. State lives in SQLite. From here Drydock will reach **out** to the fleet: SSH to Hetzner VPSs (Docker + Traefik), REST to a registrar (Porkbun) + Cloudflare, and pull images from GHCR.

```
          ┌──────────── Drydock.Api (single host) ─────────────┐
operator →│ Control plane /api/*   ← React dashboard (wwwroot) │
          └───────┬───────────────────────────┬────────────────┘
            Infrastructure                Persistence
            (clock; next: SSH /           (EF Core + SQLite)
             Hetzner / Porkbun /                │
             Cloudflare adapters)               │
                    └──────────┬────────────────┘
                     implement ports declared in
                     Application (MediatR use cases)
                               │ depends on
                    Domain (entities, enums, Result — zero infra)
```

## The five layers (`engineering/codebase/drydock.backend-services/`)

5-layer Clean Architecture per `wow-two-ws/conventions/development/backend/service-architecture.md`.

**1 · `Drydock.Api` — host & composition root.** The only layer that knows HTTP + DI. Slim `Program.cs`; wiring in `Configurations/HostConfiguration*`. Boot applies EF migrations (`AppInitialization`). Controllers: `ServersController` (register + list), `SystemController` (health). Controllers send MediatR requests via `ISender` and `Match` the `Result`; `ResultError` → HTTP status via `ApiResults`. Serves the SPA (default files + static + SPA fallback).

**2 · `Drydock.Application` — use cases & ports.** MediatR handlers (commands/queries), no infrastructure. Servers: `RegisterServerCommand` / `ListServersQuery`. Ports: `IServerStore`, `IClock`. Outcomes as `Result` / `Result<T>`.

**3 · `Drydock.Domain` — the core.** Five domain modules, each entity + enums: `Server`, `Product`, `Deployment`, `ManagedDomain`, `SecretEntry` (all `IKeyedEntity<Guid>`). `Result` / `ResultError` envelope under `Results/`. No infrastructure.

**4 · `Drydock.Infrastructure` — technical adapters.** `SystemClock` today. Next: SSH executor (SSH.NET), Hetzner / Porkbun / Cloudflare clients, GHCR pull — all behind Application ports.

**5 · `Drydock.Persistence` — storage adapter.** `DrydockDbContext` (SQLite), design-time factory, EF store (`EfServerStore`), migrations applied by `MigrateAsync()` on boot (never `EnsureCreated`).

## The 5 things Drydock manages

| Domain | Holds | Key actions |
|---|---|---|
| **Products** | name, slug, front/back repo, stack, status | register, scaffold, archive/kill |
| **Servers (VPS)** | host/IP, SSH key ref, Docker info, capacity | add, test connection, health |
| **Deployments** | product × server × env, image tags, status, logs | deploy, rollback, restart, teardown |
| **Domains** | name, registrar, expiry, DNS provider, assigned product | search, buy, point DNS, assign, renew-alert |
| **Secrets** | scope (global/server/product/env), key, encrypted value | set, inject at deploy, rotate, audit |

> v1 wires **Servers** end-to-end (register + list). The other four exist as Domain entities + DbSets; their Application/Api verticals come next.

## Deploy flow (the core mechanic — P1)

```
push → GitHub Actions builds web + api images → GHCR (tag = SHA)
  ▼ operator clicks Deploy (or Action webhook) → queue job for product × server × env
  ▼ render docker-compose.yml (image tags, Traefik labels w/ assigned domain, injected secrets)
  ▼ SSH (SSH.NET) → scp compose → `docker compose pull && up -d`
  ▼ Traefik detects host label → routes domain → issues / renews Let's Encrypt SSL
  ▼ health check /health; status + streamed logs (SignalR) to dashboard.  Rollback = re-pin prior SHA
```

## Domain flow (P2)

Search (Porkbun `domain/check`) → buy against pre-funded balance → set nameservers → Cloudflare A-record → VPS IP → assign domain → product/env → next deploy writes the Traefik `Host()` rule → cert issued.

## API surface (current)

| Method | Route | Purpose |
|---|---|---|
| GET | `/health` | Liveness |
| POST | `/api/servers` | Register a server (duplicate host → 409) |
| GET | `/api/servers` | List servers |

> The control plane is currently **unauthenticated** — it relies on network isolation (single instance, no public ingress; bind to loopback, reach over Tailscale / SSH tunnel). Auth is a hardening step — see `../planning/planning.md`.

## Security model

Drydock will hold the highest-value secret set in the portfolio — VPS SSH keys, registrar billing API, Cloudflare token, GHCR PAT. Secrets encrypted at rest (AES column converter now → migrate to the `wow-two-platform.secrets-vault` service). Scoped tokens (Cloudflare per-zone, GitHub GHCR-read). Never on the public internet — Tailscale / SSH tunnel. Audit log on every deploy, secret change, domain purchase.

## Cross-cutting

- **Data:** SQLite + EF Core; forward-only migrations on boot (`conventions/development/backend/database.md`). Swap the provider in `Persistence/DependencyInjection.cs` for Postgres if multi-instance is needed.
- **Jobs (P1):** Hangfire — deploys are long-running, need retry + a dashboard.
- **Live logs (P1):** SignalR — stream deploy/build stdout to the console widget.
- **Deploy:** one Docker image (SPA → API `wwwroot`), loopback-only — see `../deployment/`.

## Ecosystem fit

A `wow-two-platform` service — it manages the *other* products, so it sits above them operationally. Backend mirrors the `wow-two-platform.secrets-vault` sibling (slim host + MediatR CQRS + EF/SQLite). **Migration target:** `WoW.Two.Sdk.Backend.Beta` (hosting/observability/problemdetails/mediator) once a restore-verified spike confirms it; build-locally-then-migrate is the rule.

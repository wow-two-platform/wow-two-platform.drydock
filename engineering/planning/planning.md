# Drydock — Engineering Planning

*Last updated: 2026-06-12*

> Eng-side roadmap + component tracker. Product milestones / phasing live in `../../product/planning/planning.md`; this is the build view.

## Versions

Execution roadmap — granular ≤1-week versions that roll up to the phases below. Per-version detail in `../versions/v{X.Y}/v{X.Y}.md`. Bump: pre-MVP `+0.1` per capability; **`v1.0` = MVP-complete** (deploy slice live). Plan only the next version — the rest is [`backlog.md`](backlog.md).

| Version | Theme | Deliverables | Status |
|---|---|---|---|
| `v0.1` | First deploy to a real box | — | ⏳ Planned |

> P0 (skeleton + Servers vertical, ✅) predates version docs — no `v0.x` row.

## Roadmap (phase view)

Macro phases the versions roll up to. P1's deploy slice spans `v0.1` → … → `v1.0` (MVP).

| Phase | Scope | Status |
|---|---|---|
| P0 — Foundations | Clean-Arch skeleton, slim host, EF SQLite, Servers registry (register + list), dashboard shell on `@wow-two-beta/ui`, single-host Docker image | ✅ Done |
| P1 — Deploy (core) | SSH executor (SSH.NET) → render compose → `docker compose pull && up -d`; live logs (SignalR); deploy history + rollback; Traefik + Let's Encrypt on the box; GHCR pull | 🚧 Active — `v0.1` spikes the deploy thread |
| P2 — Domains | Porkbun search/buy + Cloudflare A-record + assign → Traefik route. **Folded into the MVP track (≤ `v1.0`)** per spec §7. | ⏳ Planned |
| P3 — Ops | health/uptime + Telegram alerts, SSL/domain expiry, per-product DB + backups, cost-per-product | ⏳ Post-MVP |
| P4 — Accelerate | 0→live scaffold (template → GitHub API → CI → domain → deploy), Hetzner cloud-init provisioning, teardown/kill-gates | ⏳ Post-MVP |

## Component tracker

| Component | State | Notes |
|---|---|---|
| Clean-Arch backend (slim host, MediatR, EF/SQLite) | ✅ | Mirrors secrets-vault scaffold |
| Servers vertical | ✅ | Register + list wired end-to-end; duplicate host → 409 |
| Products / Deployments / Domains / Secrets verticals | ☐ | Domain entities + DbSets exist; Application/Api next |
| Deploy executor (SSH.NET + compose render) | ☐ | P1 — spike first (`v0.1`) |
| Live logs (SignalR) | ☐ | P1 (`v0.1`) |
| Dashboard on `@wow-two-beta/ui` | ✅ | Shell wired |
| Backend SDK adoption (`WoW.Two.Sdk.Backend.Beta`) | ☐ | Migration target; spike-gated |
| Persistence | ✅ | EF Core + SQLite |

## Decisions

| Decision | Rationale |
|---|---|
| Mirror the secrets-vault Clean-Arch scaffold | Proven sibling pattern; builds clean today without the maturing beta backend SDK |
| DB → Postgres (SQLite interim) | Target is **Postgres** via the bespoke **smart-qr migration layer** (→ backend-beta); adopt when extracted. SQLite + EF migrations is the interim so iterations aren't blocked — the vertical (entities/stores/handlers) is provider-agnostic, only Persistence wiring + migrations change. |
| Single-host topology (default) | One image, .NET serves the SPA; matches drydock + secrets-vault. 2-container is the exception, expressed via a manifest |
| CI builds + publishes images; Drydock ships them | GitHub Actions → GHCR (spec §2). Drydock resolves the latest *ready* image (release info) + pushes it — it never builds. Verification = image exists, not Dockerfile valid |
| Infra extracts to the beta SDK promptly | Build inline to move fast, extract in the +0.1 once proven; products hold business logic only. Identity: `v0.1` inline → `v0.2` extract |
| Publish contract (detect + resolve) | Marker `.github/workflows/publish-docker-image.yml` = repo is publishable; image = `ghcr.io/{owner}/{repo}:{tag}`, `tag` = latest release tag (the app's product version, e.g. `v1.0.0`). Iteration 3 resolves against this. |
| Session via HttpOnly cookie, not JWT | Same-origin SPA+API → an HttpOnly encrypted cookie is XSS-safe + needs no token plumbing; it's an auth ticket, **not** server-side Session state. JWT is reserved for non-browser clients (CLI, service-to-service), available via the beta SDK identity at extraction. |

Open: registrar (Porkbun vs Namecheap) · build bespoke vs wrap Coolify · secrets→vault migration · beta SDK adoption · Postgres trigger. See [`backlog.md`](backlog.md) → Open decisions.

## Log

- **2026-06-12:** Repo conformed to the product-repo structure standard (`product/` + `engineering/`, dot-prefixed `drydock.{backend,frontend}-services/`). Domains folded into the MVP track (≤ `v1.0`) per spec §7. Backlog seeded from spec + roadmap.
- **2026-06-12:** Pivoted to the **image-consumer** model — CI builds + publishes; Drydock resolves the latest ready image + ships it (no build/scan). Single-host confirmed as default. `v0.1` re-scoped → sign-in + register + find-ready-image. Identity-extraction set as `v0.2` (+0.1). Dockerfile-scan demoted to escape hatch.

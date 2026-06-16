# Drydock — Backend Development Guidelines

*Last updated: 2026-06-12*

> **Defer to the shared conventions** in `wow-two-ws/conventions/development/backend/` — do not restate them. This file holds only Drydock deltas.

## Shared (read these in `wow-two-ws/conventions/development/backend/`)

- `service-architecture.md` — 5-layer Clean Arch
- `host-configuration.md` — slim `Program.cs` + `HostConfiguration`
- `code-organization.md`, `models.md`, `entities.md`, `enums.md`
- `result-pattern.md`, `api-endpoints.md`, `database.md`, `data-access.md`

## Repo-specific deltas

- **Mediator + Result are local for now.** v1 uses raw **MediatR 12** + a local `Result` / `ResultError` envelope (`Drydock.Domain/Results/`) — the proven secrets-vault scaffold pattern so the build is clean today. Do **not** add `WoW.Two.Sdk.Backend.Beta` until a restore-verified spike confirms its hosting/mediator/problemdetails helpers; that is the migration target, not the current state.
- **Entities** implement the local `Drydock.Domain/Common/IKeyedEntity<Guid>`.
- **Errors:** handlers return `Result` / `Result<T>` carrying a `ResultError`; controllers `Match` and map via `Api/ApiResults.cs` → HTTP status (e.g. duplicate host → 409).
- **Controllers** are thin: send a MediatR request via `ISender`, `Match` the `Result`. No logic in the host.
- **DB:** SQLite (`drydock.db`); EF migrations forward-only, applied on boot (`AppInitialization`), never `EnsureCreated`. Local EF tool pinned in `.config/dotnet-tools.json` (`dotnet tool restore`).
- **Ports:** HTTPS 8210 / HTTP 8211 (dev) — even HTTPS + odd HTTP adjacent.
- **Outbound adapters** (SSH.NET, Hetzner / Porkbun / Cloudflare, GHCR) live in `Infrastructure` behind Application ports — never called from controllers or the Domain.

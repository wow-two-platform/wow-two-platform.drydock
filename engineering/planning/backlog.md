# Drydock — Backlog

*Last updated: 2026-06-16*

Deferred work + known issues. Version docs (`../versions/`) stay clean — items land here, not there.
**Ordered queue: top = next to pull.** Grouped by theme; ordered within. Type: `feature` · `issue` · `check` · `idea`. Order is the priority — no future-version tags. Strike-through + ✅ when done (kept for traceability).

## Extract to the SDK (the +0.1 rhythm — infra proves in Drydock, then leaves it) — **v0.2**

> Full-solution scan 2026-06-16. The SDK **already ships most of this** → for most items the extract is *adopt-and-delete the inline copy*. Legend: **[adopt]** SDK has it, delete inline · **[Δ]** upstream drydock's extra behavior into the SDK type · **[new]** net-new SDK leaf. Ordered simplest/most-independent → dependent-last.

| # | Extract (drydock type) | → SDK area | Kind |
|---|---|---|---|
| 1 | `IClock`/`SystemClock` | `Foundation.Time` | adopt ⚠️ NodaTime collision |
| 2 | `IEntity`/`IKeyedEntity<TKey>` | `Data.Abstractions` | adopt |
| 3 | `AddGitHubAuthentication` | `Identity.OAuth.GitHub` | adopt + Δ (`configure` overload) |
| 4 | `FailureCategory` + `ToStatusCode` + `IDrydockFailure` | `Web.Results` + `Mediator.Result` (`ICategorizedFailure`) | new |
| 5 | `ApiResponse<T>` envelope | `Web.Contracts` | new |
| 6 | cookie secure-defaults + XHR 401/403 | `Identity.Cookies` | adopt + Δ (ApiMode) |
| 7 | `AuthSettings` allowlist + `GitHubOAuthSettings` | `Identity.OAuth.Allowlist` | new |
| 8 | default-deny cookie fallback policy | `Identity.Authorization` | new |
| 9 | `EfServerStore`/`EfProductStore` → generic repo | `Data.EntityFrameworkCore` (`EfRepository`) | adopt |
| 10 | `DateTimeOffset`→binary SQLite convention | `Data.EntityFrameworkCore` | adopt + Δ |
| 11 | migrate-on-boot + design-time factory | `Data.Migrations.Ef` | adopt |
| 12 | slim host wiring (`Program.cs`/`Api/Configurations`) | `Meta` (`AddApiDefaults`/`UseApiDefaults`) | adopt (partial) |
| 13 | single-host SPA serving + `/api/*` JSON-404 | `Web.Hosting` (new `Spa` leaf) | new |

**Stays (business logic, never extracted):** the 5 domain aggregates + enums · all `Products`/`Servers` CQRS verticals + DTOs · `ProductValidation` · `IGitHubClient`/`GitHubClient` (request-scoped repo-existence probe — product-specific) · store *interfaces* (+ `Exists*` predicates) · DbContext entity mappings · controllers · settings *values* + `launchSettings`.

**Decide once (every portfolio product hits it):** `IClock` is `DateTimeOffset`-based in drydock but **NodaTime** in the SDK — adopt NodaTime (+ touch the ~3 handlers reading `clock.UtcNow`) OR have `Foundation.Time` expose a `DateTimeOffset`/`TimeProvider` clock so adoption is a pure delete.

## Make products publishable (prerequisite for the whole deploy model)

| Item | Type | Notes |
|---|---|---|
| Reusable image-publish CI workflow (GitHub Actions → GHCR) | feature | Build the single-host image on release → push to registry. Without it Drydock has no ready image to resolve. Lives in `…pipelines` / per-repo. Do early. |
| Image-naming + tag-resolution convention | check | `ghcr.io/{org}/{repo}` + tag from release (vs main-SHA). Settles how Drydock resolves "latest ready". |

## Deploy slice — finish the core (the road from v0.1 to MVP)

| Item | Type | Notes |
|---|---|---|
| Verify a server is reachable + Docker-ready | feature | Connection/Docker test; prerequisite to any push. (Was scoped into v0.1, pulled out.) |
| Push a product's ready image to a server | feature | SSH → render deploy compose → pull & up. The actual deploy. (Was v0.1.) |
| Serve a deployed product over a real domain with automatic HTTPS | feature | Closes the "real domain + SSL" half of the milestone; assumes a domain already owned + hand-pointed. |
| Persisted deploy history per product-on-a-server | feature | Who / when / what per deploy — the record rollback selects from. |
| One-click rollback to a previous version | feature | Re-pin the prior released image; core ask. Needs history above. |
| Restart / redeploy / teardown a running product stack | feature | Round out deploy lifecycle controls. |
| Tests around the deploy executor | issue | First logic worth locking down; scaffold has none yet — add with the executor. |

## Image resolution & fallback verification (deeper layers beyond v0.1's resolve-latest)

| Item | Type | Notes |
|---|---|---|
| Multi-environment / channel resolution (release vs main-SHA, prod vs staging) | feature | v0.1 resolves one latest-release image; this generalises. |
| Multi-image / two-container topology support | feature | When a product genuinely needs a separate frontend; single-host is the default. |
| `drydock.yml` manifest standard | feature | Declare services (name·role·image·port) for multi-service / non-conformant products — the override. |
| (fallback) Scan/parse a Dockerfile when a repo has no CI image | check | Escape hatch only — demoted; the CI-image model makes this rare. |
| (fallback) Clone + real `docker build` verify | check | Escape hatch for non-CI products; needs git + token on the box. |

## Domains — in the MVP track (≤ v1.0)

| Item | Type | Notes |
|---|---|---|
| Search domain availability + price from the dashboard | feature | Registrar lookup. |
| Buy a domain against a pre-funded balance | feature | Records cost + expiry. |
| Point a bought domain's DNS at a server + assign it to a product | feature | DNS record → server; next deploy routes the host + issues its cert. |

## Harden for real use (around MVP)

| Item | Type | Notes |
|---|---|---|
| Multi-user — host one Drydock for many users | feature | Base model is **self-hosted: the signed-in GitHub user _is_ the user** (no allowlist, no org gating — anyone can run it). For a shared hosted instance: a **`Users`** table + **`OwnerId`** scoping on Product/Server/Deployment so each user sees only their own. |
| MFA / WebAuthn | idea | The beta SDK already ships `Identity/Mfa/WebAuthn` — wire when exposure warrants. |
| Delete-confirm via `@wow-two-beta/ui` modal | idea | Product delete confirmation is inline in the card; swap to a proper modal dialog from the beta UI lib. |
| Migrate secrets at rest to the secrets-vault service | check | Drydock grows NO secrets handling of its own — `wow-two-platform.secrets-vault` owns it; wire when it's ready. |
| Adopt the backend beta SDK (host + pipeline) | check | Move host + pipeline onto `WoW.Two.Sdk.Backend.Beta` once a restore-verified spike confirms its hosting/observability/mediator helpers. (Identity slice already scheduled in `v0.2`.) |

## Ops & alerts — post-MVP

| Item | Type | Notes |
|---|---|---|
| Health / uptime monitoring with channel alerts | feature | Notify when a product falls over. |
| Domain + certificate expiry alerts | feature | A dead domain = a dead product. |
| Per-product managed datastore + scheduled backups | feature | Every product eventually needs a DB + backups. |
| Cost per product (server share + domain) | feature | Feeds the micro-SaaS kill-gates. |
| Adopt Postgres + the smart-qr migration layer | feature | Target DB = **Postgres** via the bespoke smart-qr migrator (→ backend-beta). SQLite is the interim; swap the provider + migrator once it's extracted. |
| Local SQLite store hygiene — document / automate a reset | issue | Dev db accumulates throwaway rows. |

## Accelerate & lifecycle — post-MVP

| Item | Type | Notes |
|---|---|---|
| 0→live scaffold: create repo → wire CI (incl. image-publish) → first deploy | feature | The portfolio accelerator for 50–100 launches. |
| Teardown / kill-gate automation: stop, final backup, archive, release domain | feature | Executes the micro-SaaS kill gates cleanly. |
| One-click server provisioning + first-boot bootstrap | feature | New box from a single provider call. |
| Capacity / placement view across servers | feature | Bin-pack products onto boxes. |
| Staging / preview environments per product | feature | Branch deploys. |
| Privacy-friendly analytics auto-wire | idea | GWDNBM — no creepy tracking. |
| Reusable stack presets | idea | Define a stack shape once, reuse across the portfolio. |
| Full cross-action audit log | feature | Every deploy, secret change, domain purchase — beyond deploy history. |

## Open decisions

| Item | Type | Notes |
|---|---|---|
| Image resolution source — release vs main-SHA | check | Lean **release** = intentional ready version (matches "push ready code"). |
| Build the deploy substrate vs wrap an existing self-hosted PaaS | check | Bespoke = the real platform; wrap = faster fallback if the deploy spike stalls. |
| Registrar choice — cleanest-API vs familiar-but-stricter | check | Settle before the Domains group is pulled. |
| One big server vs one-per-product placement | check | Decides multi-server priority; tied to how many boxes exist now. |

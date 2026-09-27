# Feature completeness — vectors, reliability and shipping Wheelhouse

*Last updated: 2026-09-28*

What Wheelhouse needs before it is complete and reliable enough to run the portfolio: the five vectors the product
named (topology, secrets, domains, portfolio, service map), the reliability properties, how Wheelhouse ships itself,
and a sweep of gaps. This is analysis. Each decision is a box in [Points](#points); a settled point moves into a
version track. Platform-service decisions stay in [deployment topology](deployment-topology.md) points 5–11, 15, 23
and 25.

## Status

- [x] Swept: backend, runner, frontend, docs, backlog, pilot plan and the original spec (`wow-two-ws/ideas/wheelhouse-spec.md`).
- [ ] Points decided.
- [ ] Version tracks written from the decided points.

---

## Current state

| Vector | Built | Missing |
|---|---|---|
| Deployments | `dev`/`test`/`prod` targets, releases and commit builds, prod gate, locks, health gates, rollback, reconcile, history, site routes | Step log and live progress, ingress probe, notifications, stop/start, teardown |
| Topology | Traefik ingress on the local server, one `platform` network, per-environment databases created by `rehearse.py` | Host preparation, platform PostgreSQL with a database and role per target, derived settings, per-target networks, backups |
| Secrets | Vault console: namespaces, write-only values, product tokens, rotation hygiene; required-key checks on settings files | Settings rendering, deploy-time tokens, SDK vault consumer, expiring tokens, Wheelhouse's own credentials at rest |
| Domains | Site hosts per target (named or pattern); `ManagedDomain` placeholder entity | Inventory, registrar sync, DNS plan and apply, expiry tracking, preview wildcard |
| Portfolio | Product create, edit and delete (slug, name, repository, status) in the database | One product catalog, lifecycle actions, cost, capacity, onboarding |
| Service map | Per-target map from the deployed `compose.json`: services, networks, volumes, dependencies, container vitals | Sites, platform needs, service versions, environment compare, host view |
| Operations | On-demand host and container vitals, 30-day deploy metrics, one attention list | Vitals history, alerts, notifications, log tail, uptime probes |
| Wheelhouse itself | Production image, local Compose file, `rehearse.py console` | CI, its own descriptor and releases, a host, bootstrap, backups |

---

## What complete means

- The SDK doctrine applies to Wheelhouse: each vector gets every capability, not only ForeverPin's slice.
- Each capability runs end to end on the local server before a VPS exists.
- Each capability has coverage in the tier it touches: runner tests, backend E2E, frontend tests.

Reliable means seven properties:

| # | Property | Today |
|---|---|---|
| R1 | Every mutation is serialized, health-gated and recoverable without the dashboard | Built |
| R2 | Every operator action leaves an audit record | Deploy jobs only |
| R3 | A deploy shows its steps while it runs and verifies the path users take | Outcome and reason only |
| R4 | Desired state lives in code; Wheelhouse reports drift and repairs it on request | Release drift only |
| R5 | Failures reach the operator without a page open | Missing |
| R6 | Data survives a lost host: encrypted off-provider backups with a drilled restore | Runbook only |
| R7 | Wheelhouse itself is tested in CI, backed up and replaceable from the laptop | Runner CLI only |

---

## Topology — host platform services

The platform decisions live in the topology doc. Once they settle, Wheelhouse builds:

| Capability | Design |
|---|---|
| Host preparation | `host.py prepare <server>`, idempotent: Docker, deploy account, protected root, firewall, Tailscale, `platform` network, Traefik, PostgreSQL; rehearsed on the local server |
| Platform services as code | A server lists its platform services in `fleet.py`; the runner deploys them as the Compose project `platform` under the same lock and health gates |
| Databases | `needs: [postgres]` makes the runner create `<product>_<env>` and its role on first deploy; the password never leaves the host |
| Derived settings | The runner writes connection strings, `AllowedHosts` and public URLs into the settings file; operators supply only secrets |
| Networks | One network per target plus `platform`; a dev service cannot reach a prod service |
| Lifecycle | Stop, start and teardown per target under the target lock; teardown takes a final backup and the typed target ID |
| Capacity gate | A deploy is refused when the host's summed memory limits exceed its budget |
| Backups | restic per host: database dumps and named volumes, encrypted, off-provider; a restore drill into the local server |

The earliest safe live point follows this vector: host preparation, platform PostgreSQL and backups make a real host
repeatable. Today a VPS needs hand-placed Traefik, PostgreSQL and settings files.

---

## Secrets

A VPS target reads hand-placed settings files; Wheelhouse checks their keys but never writes them.

| Capability | Design |
|---|---|
| Settings rendering | The runner writes each service's settings file (`0600`) from code-owned target values, derived topology values and the vault token |
| Product secrets | Products resolve secrets from the vault at startup through the backend SDK vault consumer; Wheelhouse never reads a value |
| Deploy-time tokens | A deploy mints the target's vault token when it is missing and writes it into the settings file; it is never displayed |
| Expiring tokens | The vault mints tokens with an expiry; hygiene flags them before they lapse (vault API change) |
| Required-secret preflight | The target check confirms the vault namespace holds every key the service requires |
| Wheelhouse credentials | SSH keys, vault administrator passwords and the GitHub credential are encrypted at rest; SSH keys rotate through a runner action |
| Scoped management credential | Replaces the shared vault administrator password (vault-side change) |
| Audit | Every secret write, rotation, token mint and revoke is an audit row; values never are |

---

## Domains

| Capability | Design |
|---|---|
| Inventory | Domains synced from the registrar: expiry, auto-renew, nameservers; assigned to targets through code-owned site hosts |
| DNS plan and apply | Records derive from target site hosts and server addresses; Wheelhouse shows the plan (create, change, delete) and applies it with a per-zone token |
| Preview wildcard | One wildcard record per environment and server under the preview domain (topology point 25) |
| Certificates | Traefik issues them; Wheelhouse probes each site's certificate expiry |
| Expiry alerts | Domains and certificates at 30, 14 and 7 days |
| Pinned domains | A domain printed on physical goods (ForeverPin redirects) is pinned and cannot be reassigned or released |
| Local provider | A no-op DNS provider for `*.localhost`, so the whole flow runs locally |
| Purchase | Registrar search and purchase against a pre-funded balance, after the inventory works |

---

## Portfolio

| Capability | Design |
|---|---|
| Product catalog | One code-owned `catalog.py`: slug, repository, build workflow, registry, descriptor path; `fleet.py` targets and `artifacts.py` sources read it |
| Operator metadata | The database keeps status, notes, costs and kill-gate metrics, keyed by slug |
| Portfolio matrix | Products × environments: release, service versions, health, sites, last deploy |
| Lifecycle | Pause stops every environment; kill tears down with a final backup and archives the repository |
| Cost | Server monthly cost in `fleet.py`, domain cost from the registrar, allocated to products by memory share |
| Capacity and placement | Each host's memory budget against the declared limits of its targets; the view shows which host has room |
| Onboarding | Zero-to-live: `create-repo` scaffold with `deploy.yml` and CI, a catalog entry, the first dev deploy |

Adding a product today takes three edits (`artifacts.py`, `fleet.py`, the database row), a Wheelhouse rebuild,
hand-placed settings files and a manual database. At the portfolio's target of 50–100 launches, onboarding cost dominates.

---

## Product services map

| Capability | Design |
|---|---|
| Sites | Each site is an ingress node: host, path, exposure and the service it routes to |
| Platform needs | PostgreSQL, Valkey or broker nodes from the manifest's `needs`, shared across the host |
| Service versions | Each node carries the release in which its service last changed |
| Environment compare | `dev`, `test` and `prod` side by side; services whose versions differ are marked |
| Host view | Every target on a host, its platform services and memory allocation against capacity |

---

## Operations

| Capability | Design |
|---|---|
| Step log | The runner appends timestamped steps to the job record (validate, pull, apply, each health gate, smoke, routes); the UI polls it while the job runs |
| Ingress probe | After routes publish, the runner requests every site through the ingress by host name; a failure fails the deploy |
| Log tail | The last 200 lines of one service's logs, read-only, through the runner |
| Vitals history | A sampler keeps 30 days of host and container vitals |
| Alerts | Site down, disk above 85%, backup older than 26 hours, domain or certificate expiring, failed deploy |
| Notifications | Alerts and deploy outcomes to one channel |
| Image cleanup | A scheduled job deletes `sha-*` candidate images older than 14 days |

Uptime probes must run outside the product host's failure domain; the Wheelhouse host below provides that.

---

## Shipping Wheelhouse

Wheelhouse ships like a product: a catalog entry, a `deploy.yml`, candidate and release builds and a code-owned target.

| Concern | Design |
|---|---|
| Host | A small control VPS, Tailscale only, no public ports, separate from product hosts |
| Access | `tailscale serve` gives HTTPS on the tailnet name; a GitHub OAuth app registered for that URL; the allowlist names the owner |
| Image | The existing `engineering/deployment/Dockerfile`; one service `console`, volumes `keys` and `deployments`, `needs: [postgres]` |
| CI | `ci.yml` runs the backend tiers, runner tests and frontend typecheck, tests and build; `publish-docker-image.yml` follows the descriptor convention |
| Bootstrap | Prepare the control host, submit the first release from the laptop with `transport.py`, copy the inventory once over SSH |
| Updates | Wheelhouse deploys its own releases; the target-side runner completes while the container is replaced |
| Break-glass | The laptop keeps the operator CLI and an inventory copy; it deploys or rolls back Wheelhouse and every product |
| Backups | A nightly encrypted dump of Wheelhouse's database plus the inventory and key volumes, off-provider |
| Local rehearsal | The local server gets a `wheelhouse-dev` target; Wheelhouse deploys Wheelhouse, replacing `rehearse.py console` |
| Production settings | `AllowedHosts` names the tailnet host; `Deployment:TrustedProxies` names the address `tailscale serve` connects from, so OAuth callbacks keep `https` |

Placement options:

- Laptop: no cost; alerts and uptime probes stop whenever the laptop sleeps.
- Product host: no extra cost; every SSH key sits beside public workloads and fails with them.
- Control host: a few euros a month; it survives a product host failure and probes it from outside.

---

## Sweep — gaps and defects

| # | Finding | Evidence | Lands in |
|---|---|---|---|
| S1 | Product identity lives in three places; adding a product takes three edits and a rebuild | Database `products`, `artifacts.py` `SOURCES`, `fleet.py` targets | Point 1 |
| S2 | No audit trail of operator actions | Jobs record `actor`; vault changes reach only the app log (`VaultChangeCommandHandler.cs:17`); build requests and product edits keep no actor | v0.4 |
| S3 | A deploy shows only its outcome and reason, never its steps | The job record holds status, failure, reason and timestamps | v0.4 |
| S4 | Nothing requests a published site through the ingress | Smoke runs `compose exec <service> curl http://localhost:8080<path>` inside the container | v0.4 |
| S5 | Smoke probes and the `AllowedHosts` check assume port 8080 | `runner.py` smoke and `validate_settings`; the descriptor allows any `port` | v0.4 |
| S6 | No CI: tests never run on push and no image is published | No `.github/` in the repository | v0.4 |
| S7 | GitHub sign-in requests `repo` and `read:packages`; the runner holds a second token | `AuthConfigurationExtensions.cs`; `WHEELHOUSE_GITHUB_TOKEN_FILE` | Point 9 |
| S8 | Wheelhouse's own credentials are plain files | Inventory `ssh/`, `vaults/` and the GitHub token file | v0.6 |
| S9 | The GitHub repository is public and still named `drydock` | `wow-two-platform/wow-two-platform.drydock` | Rename handed over; Point 8 |
| S10 | Settings files on a VPS are placed by hand | Target settings are host paths | v0.6 |
| S11 | Pausing or removing an environment needs SSH | `rehearse.py down` covers only the local server | v0.5 |
| S12 | No log view; diagnosing a failed deploy needs SSH | No log action in the runner | v0.9 |
| S13 | Vitals are read on demand and nothing alerts | Backlog Hosting rows | v0.9 |
| S14 | Candidate images accumulate | The 14-day `sha-*` cleanup is specified, not built | v0.9 |
| S15 | Placeholder tables and the single-image version query remain | Backlog Cleanup | v0.4 |
| S16 | Product docs predate environments, sites, commit builds and the prod gate | `features.md`, `flows.md`, `context.md` | v0.4 |
| S17 | Old local containers run beside the rig | `drydock-pilot-*`, `foreverpin-rehearsal-*`, the old console image | v0.4 |
| S18 | Local console sign-in is still open | v0.3 Iteration 5: a second OAuth app for `:18210` | v0.3 |

---

## Build order

Local-first: each version completes on the local server. Live comes last and can move forward once v0.5 lands.

| Version | Scope | Needs |
|---|---|---|
| v0.4 Foundation | Product catalog, audit log, step log, ingress probe, port fix, Wheelhouse CI and descriptor, cleanup, docs | Point 1 |
| v0.5 Topology | Host preparation, platform services, databases per target, derived settings, networks, lifecycle, capacity gate, backups and restore drill | Topology points 5–11, 15, 23 |
| v0.6 Secrets | Settings rendering, deploy-time tokens, SDK vault consumer, expiring tokens, required-secret preflight, credentials at rest, SSH key rotation, GitHub App | Points 2, 3, 9 |
| v0.7 Domains | Inventory and registrar sync, DNS plan and apply, preview wildcard, certificate and domain expiry, pinned domains | Points 4, 5; topology point 25 |
| v0.8 Portfolio and map | Portfolio matrix, lifecycle actions, cost, capacity view, onboarding; service map sites, needs, versions, environment compare, host view | Point 1 |
| v0.9 Operations | Vitals history, alerts, notifications, log tail, image cleanup | Point 6 |
| v1.0 Live | Control host, Tailscale, OAuth app, bootstrap, self-deploy, first product host, ForeverPin live | Points 7, 8 |

---

## Points

Decide top to bottom; a parent settles before its children.

1. [ ] Product catalog: one code-owned `catalog.py` names every product; the database keeps operator metadata only.
2. [ ] Settings delivery: the runner renders settings files; products read secrets from the vault at startup; Wheelhouse never reads a value.
3. [ ] Vault placement: one vault per host, one namespace per product environment.
4. [ ] DNS ownership: records derive from code-owned site hosts; Wheelhouse plans and applies them; no hand-edited records.
5. [ ] Providers: Cloudflare DNS with per-zone tokens; the registrar after its own analysis, including payment from Uzbekistan.
6. [ ] Alert channel: a Telegram bot for alerts and deploy outcomes.
7. [ ] Wheelhouse host: a separate control VPS on Tailscale; the laptop CLI stays the break-glass path.
8. [ ] Repository visibility: private before `fleet.py` holds real host addresses.
9. [ ] GitHub access: one GitHub App replaces the sign-in `repo` scope and the runner's token file.

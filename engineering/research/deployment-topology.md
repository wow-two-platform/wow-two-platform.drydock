# Deployment topology — environments, builds, platform services and site links

*Last updated: 2026-09-27*

How wow-two products should build, version and run on Wheelhouse-managed hosts: the deployment pattern, environments
on one host, routing and site links, per-service builds and versions, builds from any commit, databases, cache,
messaging, networks, volumes and the local server. This is analysis. Each decision is a box in [Points](#points);
a settled point moves into [deployment.md](../deployment/deployment.md), the [CI policy](../planning/ci-artifact-policy.md)
or a version track.

## Status

- [x] Current model read: fleet, bundle contract, runner, rehearsal rig, pilot plan, CI policy, product stacks, Haven delivery.
- [x] Placement, environment names and site links decided (points 1, 2, 13).
- [x] Builds, versions, descriptor and build trigger decided and built (points 3, 17-22, 24, 26).
- [x] Prod gate decided and built (point 4).
- [ ] Remaining points decided.

---

## Current state

| Area | Today |
|---|---|
| Deployable | A release bundle: `release.json` + `compose.json`, digest-pinned images, health gates, no `container_name`, no host networking |
| Target | `product + environment` on one code-owned server → Compose project `<product>-<environment>`, root `/srv/wheelhouse/<product>-<environment>/` |
| Environments | `staging`, `production`, `rehearsal` in `fleet.py`; `rehearsal` names the local server, not a stage |
| Network | One external `platform` network per host; services join with alias `<product>-<environment>-<service>`; ingress and Postgres share it |
| Settings | One protected file per service, bind-mounted read-only; the bundle lists every required key |
| Services | ForeverPin: 2 images in one bundle. Haven: 6 images (5 .NET services + a Caddy edge serving 5 Vue apps), all tagged with one product version |
| Publishing | `repo-structure.md` §13 assumes one deployable image per repo, tagged with the product version |
| Data | Postgres: forever-pin, transcript-forge, tnis, transportbrain, wheelhouse, secrets-vault, haven. SQLite: sift, museums-gallery, arcade, listing-shelf, ocharo-studio, ocharo-marketing, tnis-mintrans, product template |
| Cache and messaging | No product uses Redis, RabbitMQ or Kafka. The backend SDK ships adapters for all three plus NATS; its default bus is in-memory, with an optional EF outbox |
| Releases | `main` pushes verify only; version tags publish bundles; Wheelhouse deploys the recorded digests and never triggers builds |
| Site links | Wheelhouse knows no public URL for a target; smoke probes call `http://localhost:8080` inside containers |
| Local | The rig drives the host Docker daemon; product containers have no ingress, so nothing is browsable |

---

## Decided

| # | Decision | Date |
|---|---|---|
| 1 | All environments of a product may run at the same time on one host. They need no hardware isolation; separate projects, data and hostnames are enough. Dev on the prod host exists so a collaborator can open a feature from one click | 2026-09-27 |
| 2 | Environments are `dev`, `test` and `prod`. The rehearsal rig becomes the local server | 2026-09-27 |
| 13 | Products declare public services; targets declare hostnames; Wheelhouse shows Open site | 2026-09-27 |
| 3 | Dev takes a build of any commit or branch; test and prod take published releases | 2026-09-27 |
| 4 | Prod takes a release only after it succeeded on test; a typed target ID skips the pass | 2026-09-27 |
| 17 | Branches and pull requests get their own environments later; until then branch builds land in dev | 2026-09-27 |
| 18 | One custom `deploy.yml` per product, kept as a wow-two convention for every new product | 2026-09-27 |
| 19 | A service carries the release it last changed in; Haven already shows it beside the logo | 2026-09-27 |
| 21 | Wheelhouse starts a build only for a commit that has none; images carry no environment values | 2026-09-27 |

---

## Deployment pattern

Keep the current unit: one Compose project per product-environment, built from an immutable bundle, with
target-side state owned by the runner. It bin-packs many small products per host and needs no orchestrator.

Add a platform layer per host that Wheelhouse owns and products consume:

| Platform service | When | Owned by |
|---|---|---|
| Ingress (Traefik) | Always | Host, code-owned |
| PostgreSQL | Any Postgres product on the host | Host, code-owned |
| Backup agent | Always | Host, code-owned |
| Valkey | Only for a shared need (backplane, queue) | Host, code-owned |
| Message broker | Only once two deployed services exchange events | Host, code-owned |

- Declare platform services in code beside `fleet.py`, deployed by the runner as a `platform` Compose project.
- Products declare needs in their descriptor (`needs`: `postgres`, `valkey`, `broker`).
- The runner provisions each need idempotently (database, role, vhost) before replacing containers.
- The runner writes the connection into the target settings, or refuses when a declared need has no binding.

## Environments on one host

- `<product>-dev`, `<product>-test` and `<product>-prod` are three Compose projects on the same host.
- Containers of different environments never collide: each project has its own names, network and volumes.
- Every container listens on its usual internal port (8080 for .NET); nothing publishes a host port.
- The ingress tells environments apart by hostname, so one host fits any number of environments.
- Each environment has its own database and role, settings files, vault namespace and provider keys.
- Dev and test use provider test keys (Stripe test mode); prod uses live keys.
- Memory limits come from the bundle; a runaway dev container stays inside its limit instead of starving prod.

## Routing and hostnames

```text
crm.findhaven.io                  ─┐
crm-haven.test.<preview-domain>   ─┼─ Traefik (80/443) ── Host rule ──► haven-<env>-edge:80
crm-haven.dev.<preview-domain>    ─┘
```

- The runner writes one Traefik route file per target (`ingress/<product>-<env>.yml`); Traefik reloads on change.
- A route maps each site hostname to `<product>-<env>-<service>:<port>` on the `platform` network.
- Prod hostnames come from the target: the product's own domains.
- Dev and test hostnames are generated: `<site>-<product>.<env>.<preview-domain>`.
- One wildcard DNS record per environment (`*.dev.<preview-domain>`) points at the host; no per-product DNS work.
- The flat `<site>-<product>` label keeps a later wildcard certificate possible (`*.dev.<preview-domain>`).
- Until then, Let's Encrypt HTTP-01 issues one certificate per hostname when its route appears.
- Dev and test links are unlisted but public; a product that needs a gate adds ingress basic auth per environment.
- Routes also produce settings: the runner hands each service its sites' URLs and hosts (`Sites__crm__Url`, `AllowedHosts`).
- Operators then fill only secrets; public URLs, callback origins and allowed hosts follow the target.

## Product deployment descriptor

One file per product repo, `engineering/deployment/deploy.yml`, is the single source that CI, the release generator
and Wheelhouse read. It declares the services, how each one builds, which paths change it, its sites and its needs.

```yaml
product: haven
shared:                                  # a change here rebuilds every service
  - codebase/haven.backend-services/Directory.*.props
  - deployment/backend.Dockerfile
services:
  auth:
    build: { dockerfile: deployment/backend.Dockerfile, args: { SERVICE: Auth } }
    paths: [codebase/haven.backend-services/Haven.Auth/**]
    health: /api/system/status
    needs: [postgres]
  supply:
    build: { dockerfile: deployment/backend.Dockerfile, args: { SERVICE: Supply } }
    paths: [codebase/haven.backend-services/Haven.Supply/**]
    health: /api/system/status
    needs: [postgres]
  edge:
    build: { dockerfile: deployment/edge.Dockerfile, contexts: { deployment: deployment } }
    paths: [codebase/haven.frontend-services/**, deployment/edge/**]
    health: /healthz                     # not in the Caddyfile yet
    sites:
      landing: { port: 80 }
      crm: { port: 80 }
```

- `sites` answers which ports are public: a named site on a container port; every other port stays internal.
- A service may carry several sites on one port (Haven's edge answers `landing` and `crm` by host).
- A product may make several services public (ForeverPin: `management` → `app`, `redirect` → `go`).
- `exposure: private` puts a site only on the Tailscale entrypoint (a vault console, an admin tool).
- The target maps site names to hostnames; the descriptor never holds a domain.
- `build` gives each service its own Dockerfile, target, arguments and build contexts.
- `paths` plus `shared` tell CI which services a commit changes.
- The release generator turns the descriptor into `compose.json` and `release.json`; products stop hand-writing them.
- Wheelhouse keys on this file the way it keys on `publish-docker-image.yml` today.
- Haven's edge needs a health route first: the runner refuses a service without a health gate.

## Service builds and versions

A service carries the product version in which it last changed. An old version on a service means it has not changed
since that release.

| Release | auth | supply | location | edge |
|---|---|---|---|---|
| `v1.1.0` (first) | 1.1.0 | 1.1.0 | 1.1.0 | 1.1.0 |
| `v1.2.0` (supply changed) | 1.1.0 | 1.2.0 | 1.1.0 | 1.1.0 |
| `v1.3.0` (auth and edge changed) | 1.3.0 | 1.2.0 | 1.1.0 | 1.3.0 |

- On a version tag, CI diffs the tag against the previous release tag through each service's `paths` and `shared`.
- Changed services build and publish `ghcr.io/<owner>/<repo>/<service>:<version>`.
- Unchanged services reuse the previous release's digest and version; nothing rebuilds them.
- The bundle pins every service: version, digest and the release it last changed in.
- Wheelhouse shows each environment's service versions and, before a deploy, which services will change.
- Compose recreates only services whose image or settings changed, so a deploy touches only changed services.
- The product version stream stays the version track (`vX.Y.Z`); no per-service tag namespace is needed.
- `repo-structure.md` §13 moves from one image per repo to one image per service.

## Builds from any commit or branch

- Every push, to `main` or any branch, builds candidate images for the services it changed.
- Candidate images are tagged `sha-<commit>`; unchanged services point at the last release's digests.
- A candidate is shown as `v1.3.0+<commit>`: built from that commit, changed since `v1.3.0`.
- The candidate bundle is uploaded as an Actions artifact with a 14-day retention.
- A scheduled cleanup deletes candidate images older than 14 days; release images stay.
- Any other commit builds on demand: `workflow_dispatch` with a `commit` input, started from Wheelhouse.
- Wheelhouse needs a fine-grained token with Actions write on product repositories for that dispatch.
- This reverses "Wheelhouse never triggers builds"; it still never builds on a target host.
- The catalog lists Releases (tags) and Candidates (per branch, newest first, with commit, author and time).
- Branch builds run with the repository's own `GITHUB_TOKEN` and need no product secrets.

## Branch deployments and sharing

- Now, with work on `main` only: any candidate deploys to dev in one click, and Wheelhouse copies the dev link.
- A product may let dev follow `main`: each successful `main` candidate deploys to dev automatically.
- Test takes release candidates (`vX.Y.Z-rc.N`); prod takes stable tags after a successful test deploy.
- Later, a branch gets its own ephemeral dev target: `<product>-dev-<branch>`, from a code-owned dev template.
- Its hostnames: `<branch>--<site>-<product>.dev.<preview-domain>`; its database is seeded or copied from dev.
- It stops after a quiet period and is deleted with the branch.
- PR previews reuse branch targets; the pull-request event creates and deletes them.
- Templates keep the rule that code defines every executable target; instances live in the inventory.

## Databases

- PostgreSQL: one cluster per host; one database and one least-privilege role per product-environment
  (`haven_prod`, `haven_test`, `haven_dev`).
- A cluster per product-environment would cost shared memory, a backup job and an upgrade each: about 150 at 50 products.
- The cluster publishes no port; administration goes through an SSH tunnel.
- Pin the major version per cluster; upgrade a whole cluster through dump and restore.
- Products keep migrating on startup through the SDK's bespoke migrator.
- SQLite stays the default for small single-replica products: a named `data` volume per project.
- SQLite rules: one replica only, never on a network filesystem, backups through `sqlite3 .backup`.
- A SQLite product moves to Postgres when it needs a second replica or cross-product queries.

## Cache — Redis

- Start with the in-process cache; most products never need a network cache.
- When one does, run Valkey, the BSD-licensed Redis fork; Redis 7.4 moved to source-available licenses.
- Cache-only use: a Valkey service inside the product's own bundle, no persistence, `maxmemory` with eviction.
- Shared use (SignalR backplane, distributed locks): the platform Valkey, one ACL user and key prefix per product-environment.

## Messaging — RabbitMQ, Kafka, NATS

- Default: no broker. The SDK's in-memory bus plus the EF outbox covers one deployable per product.
- A broker earns its place only when two separately deployed services exchange events.
- Then run one broker per host with one vhost or account per product-environment.
- RabbitMQ: mature, has the SDK's CAP adapter and a management UI; roughly 150 MB idle.
- NATS JetStream: one small binary, roughly 20 MB idle, also supported by the SDK.
- Kafka: excluded. It is a JVM broker with a 1 GB default heap, built for high-volume retained streams.

## Networks

- Keep one `platform` network per host: the ingress, platform data services and product services join it.
- Product services join it with their `<product>-<env>-<service>` alias; the ingress routes to those aliases.
- Each project keeps its default network for traffic inside the product.
- Only the ingress publishes ports (80/443). SSH is reachable over Tailscale or a firewall allowlist.
- Environments stay apart through credentials and aliases, which point 1 accepts as enough.
- Hardening, if ever needed: internal `data-<env>` networks, so dev containers cannot reach prod services.

## Volumes and backups

- Application state lives in project-scoped named volumes (`<product>-<env>_<name>`) declared in the bundle.
- Settings stay read-only bind mounts from the protected deployment root.
- Platform data (Postgres, Valkey, broker) lives on a dedicated block volume, resizable and independent of the server disk.
- Nightly: `pg_dump` per database plus archives of named volumes (SQLite through `.backup`).
- Encrypt with restic and ship to off-provider S3-compatible storage (Backblaze B2 or Cloudflare R2).
- Retain 7 daily, 4 weekly and 6 monthly snapshots; restore into an empty environment monthly.
- Wheelhouse reports backup age per target; the pilot's alert list already expects it.
- Teardown takes a final backup and deletes volumes only on an explicit, typed confirmation.

## Local server

- The rig becomes the local server (`VpsProvider.LOCAL`), hosting targets for every product with a local image.
- It runs dev by default; test and prod start on demand.
- Deploying prod to the local server asks for a strict typed confirmation.
- A local Traefik on `127.0.0.1:18080` reads the same route files as a VPS ingress.
- Local hostnames: `<site>-<product>.<env>.localhost`, e.g. `http://crm-haven.dev.localhost:18080`.
- Chromium and Firefox resolve `*.localhost` to loopback without DNS setup; verify Safari before relying on it.
- The local Postgres gets a database and role per product-environment through the same provisioning code.
- Local targets stay behind the `Deployment:Rehearsal` switch, so a deployed control plane never lists them.

---

## Points

Decide top to bottom; a parent settles before its children.

1. [x] Placement: all environments run at the same time on one host, without hardware isolation.
2. [x] Environment set: `dev`, `test`, `prod` replace `staging`, `production`, `rehearsal`; the rig becomes the local server.
3. [x] Release channels: dev ← a build of any commit or branch; test and prod ← published releases; promotion reuses digests.
4. [x] Prod gate: a release must succeed on test first; overriding needs a typed confirmation.
5. [ ] PostgreSQL: one cluster per host, a database and role per product-environment, provisioned by the runner.
6. [ ] SQLite stays the default for single-replica products, with nightly `.backup`.
7. [ ] Cache: in-process first; Valkey per product when needed; shared only for backplanes.
8. [ ] Broker: none by default; RabbitMQ or NATS when two services exchange events; Kafka excluded.
9. [ ] Networks: one `platform` network per host plus project networks; only the ingress publishes ports.
10. [ ] Volumes: project-scoped named volumes; platform data on a dedicated block volume.
11. [ ] Backups: restic to off-provider storage (B2 or R2), 7/4/6 retention, monthly restore drill.
12. [x] Ingress: Traefik file provider, route files written by the runner.
13. [x] Site links: products declare public services, targets declare hostnames, Wheelhouse shows Open site.
14. [x] Hostnames: prod uses the product's own domains; dev and test use `<site>-<product>.<env>.<preview-domain>`.
15. [ ] Lifecycle: start and stop per target from Wheelhouse; all environments may run at once.
16. [x] Local server: dev by default; test and prod on demand; prod needs a typed confirmation.
17. [x] Branch and PR environments: later, as ephemeral dev targets from code-owned templates; branch builds land in dev now.
18. [x] Descriptor: one `deploy.yml` per product declares services, builds, change paths, sites and needs.
19. [x] Service versions: a service carries the product version in which it last changed.
20. [x] Builds: CI builds only changed services; candidates on every push; any commit on demand.
21. [x] Build trigger: Wheelhouse starts a build only for a commit that has none.
22. [x] Candidate storage: images tagged `sha-<commit>` in GHCR; bundles as 14-day Actions artifacts.
23. [ ] Derived settings: routes produce public URLs and allowed hosts; operators supply only secrets.
24. [x] Site exposure: `public` sites route on 80/443; `private` sites only on the Tailscale entrypoint.
25. [ ] Preview domain: one domain for dev and test hostnames; which one.
26. [x] `repo-structure.md` §13: one image per service at `ghcr.io/<owner>/<repo>/<service>`.

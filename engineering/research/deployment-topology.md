# Deployment topology — environments, platform services and site links

*Last updated: 2026-09-27*

How wow-two products should run on Wheelhouse-managed hosts: the deployment pattern, databases, cache,
messaging, networks, volumes, public site links, the dev/test/prod environments and their local equivalent.
This is analysis. Each decision is a box in [Points](#points); a settled point moves into
[deployment.md](../deployment/deployment.md), the [pilot plan](../planning/deployment-pilot.md) or a version track.

## Status

- [x] Current model read: fleet, bundle contract, runner, rehearsal rig, pilot plan, CI policy and product stacks.
- [ ] Points decided.

---

## Current state

| Area | Today |
|---|---|
| Deployable | A release bundle: `release.json` + `compose.json`, digest-pinned images, health gates, no `container_name`, no host networking |
| Target | `product + environment` on one code-owned server → Compose project `<product>-<environment>`, root `/srv/wheelhouse/<product>-<environment>/` |
| Environments | `staging`, `production`, `rehearsal` in `fleet.py`; `rehearsal` names the local server, not a stage |
| Network | One external `platform` network per host; services join with alias `<product>-<environment>-<service>`; ingress and Postgres share it |
| Settings | One protected file per service, bind-mounted read-only; the bundle lists every required key |
| Data | Postgres: forever-pin, transcript-forge, tnis, transportbrain, wheelhouse, secrets-vault. SQLite: sift, museums-gallery, arcade, listing-shelf, ocharo-studio, ocharo-marketing, tnis-mintrans, product template |
| Cache and messaging | No product uses Redis, RabbitMQ or Kafka. The backend SDK ships adapters for all three plus NATS; its default bus is in-memory, with an optional EF outbox |
| Releases | `main` pushes verify only; version tags publish bundles; Wheelhouse deploys the recorded digests |
| Site links | Wheelhouse knows no public URL for a target; smoke probes call `http://localhost:8080` inside containers |
| Local | The rig drives the host Docker daemon; product containers have no ingress, so nothing is browsable |

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
- Products declare needs in the bundle (`requires`: `postgres`, `valkey`, `broker`).
- The runner provisions each need idempotently (database, role, vhost) before replacing containers.
- The runner writes the connection into the target settings, or refuses when a declared need has no binding.

## Hosts and placement

Placement is already data: `Target.server_id` binds an environment to a host, so it can change without a bundle change.

| Option | Hosts | Fit |
|---|---|---|
| A | One VPS: prod always on, test on demand; dev in the local rig | Cheapest; matches the pilot's single budget VPS |
| B | Prod VPS + nonprod VPS (dev and test); the rig mirrors both | Stable shared dev URLs; one more host |
| C | One VPS per product | Isolation nobody needs at this scale; cost grows linearly |

Recommendation: A now; move to B when a product needs a reachable dev URL (device tests, webhooks) or RAM runs out.
Wheelhouse itself stays private (Tailscale or an SSH tunnel) and never shares a host with dev workloads,
because it holds the SSH keys for every host.

## Databases

- PostgreSQL: one cluster per host, one database and one least-privilege role per product-environment
  (`foreverpin_prod`, `foreverpin_test`).
- A cluster per product would cost shared memory, a backup job and an upgrade per instance: about 150 at 50 products.
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

```text
host
├── edge            ingress + public product services (aliases <product>-<env>-<service>)
├── data-prod       internal: Postgres/Valkey/broker + prod services that need them
├── data-test       internal: the same for test
├── data-dev        internal: the same for dev
└── <product>-<env>_default   per project: service-to-service traffic inside one product
```

- Only the ingress publishes ports (80/443). SSH is reachable over Tailscale or a firewall allowlist.
- Data networks are `internal: true`: no route out, and dev containers cannot reach prod data on a shared host.
- Bundles attach logical networks: `edge` for public services, `data` for data clients.
- The runner maps `data` to `data-<env>` and replaces today's single `platform` variable.
- A later multi-host tier uses the provider's private network or WireGuard between hosts.

## Volumes and backups

- Application state lives in project-scoped named volumes (`<product>-<env>_<name>`) declared in the bundle.
- Settings stay read-only bind mounts from the protected deployment root.
- Platform data (Postgres, Valkey, broker) lives on a dedicated block volume, resizable and independent of the server disk.
- Nightly: `pg_dump` per database plus archives of named volumes (SQLite through `.backup`).
- Encrypt with restic and ship to off-provider S3-compatible storage (Backblaze B2 or Cloudflare R2).
- Retain 7 daily, 4 weekly and 6 monthly snapshots; restore into an empty environment monthly.
- Wheelhouse reports backup age per target; the pilot's alert list already expects it.
- Teardown takes a final backup and deletes volumes only on an explicit, typed confirmation.

## Ingress, domains and site links

- Traefik with its file provider: the runner writes one route file per target (`ingress/<product>-<env>.yml`).
- Traefik reloads on file change, so it needs no Docker socket.
- TLS uses Let's Encrypt HTTP-01 per hostname; Cloudflare DNS-01 adds wildcards when previews need them.
- The bundle declares public services, product-owned: `public: {management: {port: 8080}, redirect: {port: 8080}}`.
- The target declares hostnames, environment-owned: `routes=(("management", "app.<domain>"), ("redirect", "<short domain>"))`.
- Wheelhouse returns `urls` per target; the UI shows Open site on the target, the workspace and deploy success.
- After cutover a smoke probe can call the public URL once, proving DNS, TLS and routing together.
- Prod uses each product's own domain; dev and test use a shared preview domain:
  `<product>.test.<preview-domain>`, `<product>.dev.<preview-domain>`, one wildcard certificate per environment.

## Environments

| | dev | test | prod |
|---|---|---|---|
| Purpose | Latest `main`, integration | Acceptance of a candidate | Customers |
| Releases | Candidate bundle per `main` push | `vX.Y.Z-rc.N` tags | `vX.Y.Z` tags |
| Runs | On demand | During acceptance | Always |
| Data | Disposable, seeded | Copy or seed | Real, backed up |
| Providers | Test keys (Stripe test mode) | Test keys | Live keys |

- `DeploymentEnvironment` becomes `dev`, `test`, `prod`; the local rig becomes a server (`VpsProvider.LOCAL`).
- Target ids stay `<product>-<env>`, so aliases, projects and roots keep their shape.
- Promotion moves the same bundle and digests upward; only settings differ between environments.
- Prod accepts a release only after it succeeded on test; an override needs a typed confirmation.
- Each environment has its own settings files, database and role, OAuth client, vault namespace and resource limits.
- The dev channel needs the CI policy's candidate-per-`main` option: short-retention bundles named by commit.
- Wheelhouse gains start and stop per target (`compose stop/start` under the same lock) to keep test and dev off when idle.

## Local environments

- The rig becomes a local server hosting `dev`, `test` and `prod` targets for every product with a local image.
- A rig Traefik on `127.0.0.1:18080` reads the same route files from the rig's deployment root.
- Local hostnames: `<service>.<product>.<env>.localhost`, e.g. `http://management.foreverpin.dev.localhost:18080`.
- Chromium and Firefox resolve `*.localhost` to loopback without DNS setup; verify Safari before relying on it.
- The rig's Postgres gets one database and role per product-environment through the same provisioning code as a VPS.
- Local targets stay behind the existing `Deployment:Rehearsal` switch, so a deployed control plane never lists them.
- Open site then works locally as on a VPS, and a local candidate bundle can be promoted dev → test → prod.

## PR previews (later)

- An ephemeral `<product>-pr-<n>` target on the nonprod host, built from a pull-request candidate bundle.
- Hostname `pr-<n>.<product>.dev.<preview-domain>`; stopped and deleted when the pull request closes.
- Needs CI preview bundles, wildcard DNS-01 and targets derived from a code-owned template.
- The template keeps the rule that code defines every executable target.

---

## Points

Decide top to bottom; a parent settles before its children.

1. [ ] Placement: one VPS now (prod always on, test on demand), dev in the local rig; a nonprod VPS later.
2. [ ] Environment set: `dev`, `test`, `prod` replace `staging`, `production`, `rehearsal`; the rig becomes a server.
3. [ ] Release channels: dev ← `main` candidates, test ← rc tags, prod ← stable tags; promotion reuses digests.
4. [ ] Prod gate: a release must succeed on test first; overriding needs a typed confirmation.
5. [ ] PostgreSQL: one cluster per host, a database and role per product-environment, provisioned by the runner.
6. [ ] SQLite stays the default for single-replica products, with nightly `.backup`.
7. [ ] Cache: in-process first; Valkey per product when needed; shared only for backplanes.
8. [ ] Broker: none by default; RabbitMQ or NATS when two services exchange events; Kafka excluded.
9. [ ] Networks: `edge`, internal `data-<env>` and the project default; only the ingress publishes ports.
10. [ ] Volumes: project-scoped named volumes; platform data on a dedicated block volume.
11. [ ] Backups: restic to off-provider storage (B2 or R2), 7/4/6 retention, monthly restore drill.
12. [ ] Ingress: Traefik file provider, route files written by the runner.
13. [ ] Site links: bundles declare public services, targets declare hostnames, Wheelhouse shows Open site.
14. [ ] Domains: each product's domain for prod; one shared wildcard preview domain for dev and test.
15. [ ] Lifecycle: start and stop per target from Wheelhouse; prod always on.
16. [ ] Local environments: rig targets per environment behind a local Traefik on `*.localhost:18080`.
17. [ ] PR previews: ephemeral nonprod targets from pull-request bundles, after the points above.

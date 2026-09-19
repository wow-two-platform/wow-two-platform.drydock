# Deployment pilot — ForeverPin

*Last updated: 2026-09-19*

## Outcome and ownership

DryDock governs the infrastructure portfolio. The essential slice is a repeatable, observable deployment of
ForeverPin to one already-provisioned Linux VPS. GitHub Actions builds; DryDock selects and deploys verified artifacts.
Purchasing servers, provider billing, DNS changes and production cutover belong to the subsequent wiring session.

This plan supersedes the June deployment assumptions in the old architecture and version notes.
The workspace [readiness assessment](../../../../../docs/audits/deployment-readiness-2026-09-19.md)
is the inventory baseline. Source inspection in this session confirms that the deployment entity was a placeholder:
no executor, durable worker or deployment API existed at intake.

## Essential governance boundary

| Capability | Today’s slice | Later expansion |
|---|---|---|
| Inventory | Existing products/servers; explicit host/environment binding | Provider accounts, regions, renewal costs, capacity budgets |
| Releases | Same-commit image digest map, versioned bundle, config names | Signatures, registry provenance enforcement, retention policies |
| Changes | Explicit deploy, serialized mutation, durable status | Approval policies, scheduled rollout, policy-as-code |
| Recovery | Previous bundle, schema compatibility gate, independent CLI | Automated restore drills, cross-provider disaster recovery |
| Secrets | Protected host files and pinned SSH identities | Vault integration, rotation, access audit |
| Domains | Explicit ingress routes and stable redirect origin | Registrar/DNS automation, expiry tracking |
| Operations | Readiness, deployment result, bounded logs, backup runbook | External uptime, backup age, disk alerts, billing alerts |
| Fleet | Provider-neutral SSH to one selected host | More independent hosts with placement and relocation workflows |

Automatic purchasing, unrestricted shell execution, full domain automation, multi-node scheduling and a new frontend
framework are not prerequisites for this deployment. A single VPS is still one failure domain.

## Current evidence and implications

### DryDock

- PostgreSQL and bespoke startup migrations are implemented; SQLite packaging and architecture prose were stale.
- GitHub authentication and a normalized-login allowlist exist; an empty list previously admitted every GitHub user.
- Products, servers and single-image version lookup work; the UI’s ready-image result is not multi-service release acceptance.
- SSH key references point to an unfinished secret domain; the initial runner needs explicit file-backed SSH configuration.
- The control plane remains private. Loopback binding plus an SSH tunnel is sufficient for the initial operator path.
- A stopped control plane must neither stop products nor prevent an operator deploying or recovering them.

### ForeverPin

- Management API/SPA and redirect API share PostgreSQL and apply advisory-locked migrations on startup.
- The redirect process is independently deployable and must stay available independently of management/editor failures.
- Management cookies need a stable key volume and application name across container replacement.
- Google’s client ID and redirect origin were frontend build settings. Public runtime configuration is needed for image promotion.
- Both hosts need database-aware health checks; process liveness alone does not establish a working redirect.
- The analytics queue is in memory and drops under pressure. Graceful drain can reduce orderly restart loss, not crash loss.
- The active product track retains unresolved dynamic content, validation/concurrency and manual QA gates.
  Shipping infrastructure must not relabel all current product features as ready.

## Release contract

A release is a trusted, versioned directory containing:

- `release.json`: schema version, product slug, release label, full source SHA, CPU platform, service image digests,
  Compose SHA-256, required setting names and whether the new schema permits the previous images.
- `compose.json`: product-owned topology, stable named volumes, resource limits and health checks.
- No credentials, environment-specific domains or database values in either artifact.

The runner verifies the Compose hash and service image map before mutation. Releases are privileged deployment code:
only a reviewed product build may create them. Hashing detects mismatched artifacts; it does not authenticate a malicious publisher.
A deployment target supplies the environment’s settings independently. Promotion changes target settings, not image bytes.
The first release defaults to no automatic rollback; subsequent releases must explicitly declare backwards-compatible migrations.

Build publication requires backend tests, frontend typecheck/tests, both images, and one manifest emitted only after both pushes succeed.
A failed second image publish may leave an unused image; it must never produce a deployable bundle.
Architecture starts with `linux/amd64`. ARM is a separate build/test target, not an automatic consequence of a cheaper VPS.

## Execution model

1. Validate the manifest, immutable digests, Compose hash and environment identity.
2. Acquire a target-side lock for the product/environment; concurrent operators use the same lock.
3. Reject an unresolved interrupted rollout until its state is inspected.
4. Check Docker/Compose, required settings, free disk and release platform.
5. Pull every image before replacing any running container.
6. Save durable deployment intent and the previous successful bundle on the target.
7. Apply Compose and wait for every declared application health check.
8. Run an application smoke gate where configured; a real redirect is a launch gate.
9. Persist success with the release identifier, source commit, actor and previous release.
10. On failure, retain the attempted release and outcome. Restore previous images only under an explicit compatibility guarantee.
    Never auto-restore a database or delete volumes.

Compose in-place replacement has a restart window. Zero-downtime blue/green routing is deferred until measured demand warrants it.
A failed first deployment may leave unhealthy containers for diagnosis. A partial failure cannot be labelled successful.

The recovery CLI and DryDock call the same target runner. SSH uses strict host-key checking, a pinned known-hosts file,
batch authentication, explicit identities and a bounded connection timeout. No automatic host-key acceptance.
The target runner owns state even if the caller disconnects. Unknown/interrupted states require observation, not blind retries.

## Host and environment model

`product + environment -> selected server + Compose project + config file + data volumes`.

The server record identifies the infrastructure asset. The target configuration records SSH identity paths, trusted host keys,
deployment root and environment settings. Secrets stay out of URLs, manifests, frontend bundles and audit messages.

Production and staging use different database/users, domains, projects, key volumes and Stripe/Google settings.
Only production stays on by default. Staging is started for acceptance and stopped afterwards.
Do not move printed URLs when selecting the redirect domain.

Initial host services:

- One ingress accepts public 80/443, obtains TLS certificates and forwards only configured hosts.
- PostgreSQL is reachable on a private Docker network, with one non-superuser role/database per environment.
- ForeverPin management and redirect join the required private networks.
- DryDock has private ingress only and its own database/key volume.
- Backups leave the provider/account boundary, encrypted with a recovery key held elsewhere.

Container limits are initial guardrails, not a capacity claim. Measure host headroom and redirect latency before admitting more products.
Do not build images on the budget VPS. Count independent backup storage, tax and public IP cost inside the approved budget.

## Wiring additional VPSs

1. Provision through the selected provider outside the deployment pipeline.
2. Verify the SSH host fingerprint through the provider console.
3. Install supported Docker Engine/Compose and Python 3; create the deployment account and protected deployment root.
4. Configure firewall/private administration, time sync, bounded Docker logs and monitoring.
5. Start host PostgreSQL/ingress; create isolated product databases and runtime setting files.
6. Register the server and a target binding in DryDock.
7. Run read-only preflight, deploy a staging release, rehearse recovery, verify backup restoration.
8. Assign production to that host only after DNS/TLS and application tests pass.

A different provider changes the server binding, not the release bundle.
Adding a host does not replicate databases, secrets or volumes. Moving a stateful product requires backup/copy,
a write-freeze or replication plan, verified restore, DNS TTL planning and an explicit cutover.
No active-active promise is made by adding a second server row.

## Shipping sequence and acceptance

- [x] Packaging: clean container builds for all three services from the working tree.
- [x] Startup: local PostgreSQL migrations and database-aware health checks.
- [x] Persistence: recreate ForeverPin containers; saved code, SVG and redirect survive.
- [ ] Release: tested workflow publishes immutable service map and matching Compose hash.
- [x] Runner: validation, locking, interrupted-state handling and failed-rollout recovery tests.
- [x] Control plane: deploy/status API, durable outcome and same independent recovery path.
- [x] Documentation: operator commands, settings contract, VPS binding and launch checklist.
- [ ] Public wiring: chosen host, pinned SSH, ingress, real domains, provider callbacks.
- [ ] Recovery: encrypted off-host backup restored into an empty environment.
- [ ] Launch: live editor/create/scan, restart, actual redirect monitoring and headroom.

Execution evidence and exact commands belong in [deployment operations](../deployment/deployment.md).
Unchecked items remain open regardless of build success.

Local evidence on September 19: 121 DryDock backend tests; 27 runner/SSH-adapter tests;
213 ForeverPin backend tests and four frontend tests. Clean images started on Docker Desktop `linux/arm64`.
A real guest-created URL code persisted across replacement, rendered SVG and returned its expected redirect.
The target runner applied digest-pinned images, recovered from an intentionally unhealthy release,
and finished a detached submission after the caller exited. A database dump restored into a new local database.
Real SSH, hosted CI, `linux/amd64`, TLS/OAuth/Stripe and encrypted off-provider backup remain unverified.

The pilot exposed a URL routing defect: URL content has no static payload encoder, but redirect resolution used
that encoder as its destination. The redirect service now reads validated HTTP(S) URL content directly.
Its HTTP tests use actual URL rules instead of the text-content workaround.

Cookie key files in both applications also retained identical hashes across container replacement.
The same management image returned different public Google configuration from a target setting file.
The bundle importer accepted the tested release archive and rejects links or duplicate bundle IDs.

## Commit coordination

- The deployment backend, runner and packaging are committed in DryDock.
- The new dashboard panel uses the existing, uncommitted UI SDK upgrade to `0.0.95`.
  That upgrade and its related UI edits predate this task; the panel waits for the coordinated frontend batch.
- ForeverPin's packaging/release batch overlaps another task's staged review improvements.
  Preserve that prepared batch; the URL-routing repair and smoke script are an additional tested batch.
- Hosted publication must include the intended combined working tree; local image builds are not evidence
  that a partial commit contains every tested frontend dependency.

## Launch decisions for the wiring session

- Existing or new host, verified architecture, available RAM/disk and authorized provider cost.
- Exact management and permanent redirect domains; existing printed-link/data recovery requirements.
- Real Google origin/client and Stripe test-mode webhook configuration.
- Recovery-point/recovery-time targets and off-provider backup destination.
- Whether this is a private pilot or a public launch with the outstanding product gates.

These inputs do not block local implementation. They do block a claim of production readiness.

## Primary references

- [Compose health waits](https://docs.docker.com/reference/cli/docker/compose/up/)
- [Service startup order](https://docs.docker.com/compose/how-tos/startup-order/)
- [ASP.NET trusted proxies](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [GitHub image publishing](https://docs.github.com/en/actions/tutorials/publish-packages/publish-docker-images)

# Deployment operations

*Last updated: 2026-09-19*

## Ownership and current boundary

GitHub Actions builds immutable images. DryDock submits reviewed release bundles over pinned SSH.
The target-side Python runner owns locks, durable intent, health gates and recovery. The same runner works without the dashboard.
Git triggers, artifact publication and retention are defined in the [CI policy](../planning/ci-artifact-policy.md).

The [pilot plan](../planning/deployment-pilot.md) owns scope, future VPS wiring and launch gates.
Local image builds use the current working tree; publishing requires all intended source/dependency changes committed together.

## Local packaging

From this repository root:

```sh
export POSTGRES_PASSWORD='<local-only generated password>'
export DRYDOCK_ADMIN='<your GitHub login>'
docker compose -p drydock-local -f engineering/deployment/docker-compose.yml up --build --wait
```

This Compose file is a local acceptance stack. Its PostgreSQL database and cookie keys use project-scoped volumes.
The API binds to loopback. Configure GitHub credentials through a protected `appsettings.Local.json` mount for real sign-in.
Production refuses an empty `Identity:AllowedGitHubLogins`.
The local HTTP endpoint is not evidence of real HTTPS/OAuth readiness.

Build context `engineering/codebase/` excludes local overrides, environment files, private keys, logs and generated output.
The Node stage builds the SPA; .NET publishes the supplied SPA without running Node again.
The runtime runs as `app` and uses PostgreSQL, not SQLite.

## Runner installation and inventory

The image includes `/app/runner/{runner,transport,fleet,artifacts}.py`.
`fleet.py` is the reviewed source of provider enums, VPS identities and deployment bindings.
No `targets/*.json` file or database server row can add an executable target.
The UI and server API are read-only; adding a host requires a code change and a new DryDock image.

Mount a protected persistent directory at `/data/deployments` containing:

```text
ssh/<server-id>/identity
ssh/<server-id>/known_hosts
bundles/<artifact-id>/{release,compose,source}.json
jobs/
observed/
```

The checked-in fleet starts empty. During wiring, add verified `Server` and `Target` values in `fleet.py`.
For example only (these are not enabled hosts):

```python
SERVERS = (Server("pilot-host", "Pilot host", VpsProvider.HETZNER,
                  "vps.example.net", "hel1"),)
TARGETS = (Target("foreverpin-staging", "pilot-host", "foreverpin",
                  DeploymentEnvironment.STAGING,
                  (("management", "/srv/secrets/foreverpin-staging/management.json"),
                   ("redirect", "/srv/secrets/foreverpin-staging/redirect.json")),
                  "platform"),)
```

Add a real redirect smoke probe to the target after the stable pilot code exists.
The default root is `/srv/drydock`; secret values never enter source, bundles or the browser.
IDs are stable lowercase slugs; never reuse a host ID for a different machine.

`artifacts.py` declares approved public GitHub repositories and exact service image repositories.
The catalog lists only published versioned release assets with completed upload and checksum metadata.
DryDock validates the archive, bundle contents and tag/source commit when a release is selected.
A GHCR pull remains the definitive image-availability check before container replacement.
Use `Deployment:GitHubTokenFile` for a mounted read-only catalog token; the operator CLI reads the equivalent
`DRYDOCK_GITHUB_TOKEN_FILE` environment variable. Anonymous GitHub requests have a lower shared-IP rate limit.
The token is sent only to the API origin and is removed from CDN redirects. Private release downloads are not yet supported.

Registry login belongs to the target deployment account. Public GHCR images need no pull credential;
private images require a read-only token and `docker login --password-stdin`.
Verify first-publish package visibility explicitly; source-repository visibility does not prove image visibility.

The target requires Linux, Python 3, Docker Engine/Compose v2, private configuration files and the external `platform` network.
A deployment account with Docker access can control the host; membership in the Docker group is privileged.

Private settings can be mode `600` owned by the image UID (`1654`) with a runner account able to read them.
Alternatively, use mode `644` inside a mode `700` directory owned by the deployment account:
the directory protects host access, while the read-only file bind is readable by the container UID.
Never use group/world-writable settings. Cookie key volumes must remain writable by UID `1654`.

## Operator and API commands

From the repository root, using the same inventory as the dashboard:

```sh
python3 engineering/codebase/drydock.runner-services/transport.py targets --root /path/to/inventory
python3 engineering/codebase/drydock.runner-services/transport.py releases --root /path/to/inventory
python3 engineering/codebase/drydock.runner-services/transport.py submit \
  --root /path/to/inventory --target foreverpin-staging --bundle <artifact-id-from-releases> --actor operator
python3 engineering/codebase/drydock.runner-services/transport.py status \
  --root /path/to/inventory --job <returned-id>
```

- `GET /api/deployments/targets`: configured target bindings.
- `GET /api/servers`: hosts defined in code; registration and deletion return `405`.
- `GET /api/deployments/releases`: published artifacts from approved release sources.
- `POST /api/deployments`: JSON `{"target":"foreverpin-staging","release":"foreverpin-v1"}`,
  authenticated admin plus `X-Drydock-Action: deploy`.
- `GET /api/deployments/{id}`: refresh the target-owned outcome.
- HTTP `202` means queued, not deployed.
- A lost SSH response is an unknown outcome; inspect target state before retrying.
- Cookie-authenticated cross-origin writes cannot supply the custom header without an allowed CORS preflight.

The dashboard polls active deployments. Restarting the dashboard does not terminate a launched target worker.
The initial dashboard displays the current submission; durable history is retained in the inventory and target job directories.
It does not yet provide a fleet-wide history browser. Artifact discovery reads GitHub releases without interacting with CI.

## Target state and recovery

```text
/srv/drydock/<product>-<environment>/
  lock
  active.json
  current.json
  jobs/<job-id>.json
  releases/<job-id>/{release,compose}.json
```

Pulls finish before replacement. Success requires healthy containers with the exact image references and configured smoke responses.
Failed image pulls preserve the running release. A failed rollout restores prior images only if the incoming bundle explicitly
declares `rollbackCompatible: true`. The default release generator leaves this false.

Database restore is never automatic. Image rollback cannot undo a destructive migration or data written after a backup.
An interrupted or unrecovered mutation blocks another deployment until the operator inspects containers/schema and acknowledges it:

```sh
python3 /srv/drydock/incoming/<submission>/runner.py status \
  --target /srv/drydock/incoming/<submission>/target.json --job <remote-job-id>
python3 /srv/drydock/incoming/<submission>/runner.py acknowledge \
  --target /srv/drydock/incoming/<submission>/target.json --job <remote-job-id>
```

Acknowledgement clears the previous-success pointer rather than assuming it is still safe for automatic rollback.
It changes bookkeeping only. Recover application/database state explicitly before acknowledging.
A retry after reconciliation establishes a new known-good release.

Raw Docker/SSH output is not returned to the dashboard because it may contain runtime secrets.
Audit records keep actor, release, source SHA, timestamps, outcome and failure category.
Inspect detailed container logs privately on the target. Runtime Docker logs rotate in the release bundle.

## VPS wiring checklist

1. Verify provider account, host architecture, capacity and cost in the wiring session.
2. Verify the SSH fingerprint through the provider console; install the pinned known-hosts file.
3. Install Docker/Compose and Python using the chosen OS's official instructions.
4. Configure private administration and firewall; expose only intended ingress on 80/443.
5. Start one ingress and PostgreSQL on the private platform network.
6. Create distinct least-privilege databases/users for every product/environment.
7. Write protected runtime settings and registry credentials.
8. Route management/redirect domains to `foreverpin-<environment>-management:8080` and
   `foreverpin-<environment>-redirect:8080`.
9. Add the ingress IP to `Deployment:TrustedProxies` in both app settings; no trust-all proxy setting.
10. Bootstrap DryDock privately or use the operator command from a workstation.
11. Deploy staging, verify real URLs and provider callbacks, restore a backup, promote the same image digests.

Existing product containers keep serving without DryDock. A new VPS requires a reviewed fleet code change and DryDock rebuild; it uses the same product release.
Data relocation remains a separately planned copy/restore/cutover operation.

## Backups and launch gates

Before public cutover, create an encrypted off-provider backup of product/control-plane databases, key volumes and required configuration.
Hold decryption and recovery credentials outside the VPS. Record recovery time/data-loss targets and backup retention.
Restore into an empty database and verify a real code resolves before accepting the backup path.

Required live checks: stable printed URL, HTTPS, Google sign-in, Stripe test callback, external redirect monitor, disk/backup-age alerts,
and measured CPU/RAM headroom. Review SDK dependency advisories reported by the clean image restore.
The separate ForeverPin product track still owns incomplete content modes and validation features.

## Verification

See the checked acceptance items in the [pilot plan](../planning/deployment-pilot.md).
Local container evidence is scoped to Docker Desktop `linux/arm64`; a hosted `linux/amd64` workflow run and real SSH/OAuth/TLS remain separate gates.

```sh
python3 -m unittest discover -s engineering/codebase/drydock.runner-services -v
dotnet test engineering/codebase/drydock.backend-services/Drydock.slnx -p:BuildSpa=false -m:1
```

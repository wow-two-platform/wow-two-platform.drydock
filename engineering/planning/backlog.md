# Wheelhouse — Backlog

*Last updated: 2026-09-29*

Deferred work; top of each group = next. Version docs hold only the active version.

## Hosting

| Item | Type | Notes |
|---|---|---|
| Prepare a VPS for deployments with one command | feature | Traefik (file provider on `/srv/wheelhouse/ingress`), PostgreSQL, `platform` network, deploy account, protected root, firewall |
| Choose the shared preview domain for dev and test hosts | check | Topology point 25; one wildcard DNS record per environment |
| Start and stop an environment from Wheelhouse | feature | Topology point 15; `compose stop/start` under the target lock |
| Encrypted off-provider backups with a restore drill | feature | Product databases, key volumes, Wheelhouse state; decryption keys held off-host |
| Uptime, backup-age and disk alerts | feature | The vitals sampler exists; needs the alert channel (completeness Point 6) and an external probe |
| Host Wheelhouse privately and let it deploy itself | feature | CI and its release workflow exist; needs a control host (completeness Point 7) |
| Retire `rehearse.py console` for the self-deployed console | check | `rehearse.py self` deploys Wheelhouse to `wheelhouse-dev`; it needs an OAuth app and the inventory mount to manage targets |

---

## Secrets

| Item | Type | Notes |
|---|---|---|
| Grant a product environment its vault token during deployment | feature | Mint, then write into the target settings; never displayed |
| Vault consumer in the backend SDK | feature | Startup resolution, bounded timeout, fail-closed; unblocks ForeverPin adoption |
| Scoped management credential for Wheelhouse | check | Vault-side change; replaces the shared administrator password |
| Mint expiring product tokens | feature | Vault API change: mint accepts only a name today; hygiene already flags expiry |

---

## Deployments

| Item | Type | Notes |
|---|---|---|
| ForeverPin adopts `deploy.yml` and the candidate workflow | feature | Local server holds its descriptor today; per-service image names via `image` |
| Haven adopts `deploy.yml`, per-service versions and an edge health route | feature | Its Caddy edge has no health route; the runner requires one |
| Show each service's version inside every product | feature | Haven shows its build version beside the logo; adopt across products |
| Deploy a branch to its own temporary dev environment | feature | Topology point 17; from a code-owned template; later PR previews |
| Browse releases older than the recent catalog | feature | The target journal already retains deployed bundles |
| Per-service versions in the release catalog | feature | The catalog lists releases; versions need each bundle's manifest |
| Delete `sha-*` candidate images older than 14 days | feature | A scheduled workflow per product; the descriptor convention names it |
| Notify deploy outcomes and alerts | feature | Needs the alert channel (completeness Point 6) |
| Signed provenance for release bundles | feature | Attestation check before selection |
| Private release-asset download | feature | Token-authenticated catalog for private repositories |
| Zero-downtime replacement | idea | Blue/green only when measured demand warrants it |

---

## Domains

| Item | Type | Notes |
|---|---|---|
| Registrar search and purchase against a pre-funded balance | feature | Registrar choice open |
| DNS records and ingress routes per product environment | feature | Per-zone scoped tokens |
| Domain and certificate expiry tracking | feature | A dead domain is a dead product |

---

## Portfolio

| Item | Type | Notes |
|---|---|---|
| Second provider and a placement view | feature | Provider enum plus integration in code |
| Cost per product and host | feature | Feeds the micro-SaaS kill gates |
| Teardown with a final backup and archive | feature | |
| Zero-to-live scaffold from the product template | feature | Repository, CI, first deployment |

---

## Historical React SDK gaps

These observations belong to the retired React frontend and are not current Wheelhouse blockers.
The September 26 [Vue migration](ui-workspace.md) uses `@wow-two-beta/ui-vue@0.0.7`;
React persistence peers, sidebar overrides, skeleton shim and exit-keyframe override were removed.
The local refresh timing helper and page-action composition remain explicit Vue product adapters.
SDK-wide follow-up ownership is independent of this migration; the original observations remain below.

| Item | Type | Notes |
|---|---|---|
| UI SDK `AlertModal.Action`/`Cancel` render as the corner close icon | issue | Fixed in the SDK working tree; tests pass in Chromium; needs a release and re-pin |
| UI SDK `query` entry imports optional persistence peers | issue | Consumers must install both persister packages; move persistence to a subpath |
| UI SDK `useAppQuery` has no polling interval | feature | Wheelhouse polls inside two hooks meanwhile |
| UI SDK `useAppQuery` exposes no background-fetching flag | feature | Added in the SDK working tree (`fetching`, `useRefresh`); needs a release and re-pin |
| Drop the exit-keyframe override in `bootstrap/index.css` | check | Masks the SDK Presence enter twitch at 0.0.108; the SDK fix is in its working tree, awaiting release and re-pin |
| Swap the skeleton and `useRefresh` shims for the SDK's | check | `presentation/common/skeleton` + `application/common/useRefresh` copy the unreleased SDK parts; delete after the re-pin |
| UI SDK `AppShell` has no user-collapsible rail or full-height sidebar | feature | Wheelhouse overrides the sidebar's classes and drawer padding |
| Move the Wheelhouse palette into the UI SDK theme registry | check | Lives in the app's `index.css` today, beside `theme-smart-qr` |
| UI SDK `Stat` trend has no inverse or custom format | feature | Durations fall as they improve; Wheelhouse's `KpiTile` composes `TrendIndicator` meanwhile |
| UI SDK `Table` has no sticky-header option | feature | Its head is translucent; Wheelhouse's `TableStyles` pins an opaque one |
| UI SDK `AppShell` has no page-header actions slot | feature | Wheelhouse portals them through `PageActions` |
| UI SDK `DropdownMenu` has no radio items | feature | The theme choice uses plain items with a check |
| UI SDK `Select` root is full-width inside a flex row | issue | The product form's provider select needed a fixed-width box |

---

## Cleanup

| Item | Type | Notes |
|---|---|---|
| Retire the placeholder server, deployment, domain and secret tables | issue | Unused since the code-owned fleet |
| Require the action header on product writes | issue | Every other write carries it; waits on the product catalog decision (completeness Point 1) |
| Retire the single-image version-status query | issue | Replaced by the published artifact catalog |
| Extract the vault admin client to the backend SDK | check | After `v0.3` proves it |
| Swap the canvas copy for the SDK's `CanvasArea` | check | `presentation/common/components/canvasArea` copies the unreleased SDK part; delete after the re-pin |
| Stamp applied migrations with the product version | issue | `MigrationOptions.Version` keeps the SDK default `v1.0` |
| Clear the transitive backend package advisories | issue | Five high, four moderate; they arrive through the backend SDK at `10.0.40-beta` |

Route splitting and skeleton first loads were completed in the [Vue workspace migration](ui-workspace.md).

---

## Open decisions

| Item | Notes |
|---|---|
| Registrar | Settle before the Domains group |
| One vault per environment or one per host with namespaces | Vault docs assume one per product environment |

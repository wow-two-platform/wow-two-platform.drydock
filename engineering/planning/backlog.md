# Wheelhouse — Backlog

*Last updated: 2026-09-26*

Deferred work; top of each group = next. Version docs hold only the active version.

## Hosting

| Item | Type | Notes |
|---|---|---|
| Prepare a VPS for deployments with one command | feature | Ingress, PostgreSQL, `platform` network, deploy account, protected root, firewall |
| Encrypted off-provider backups with a restore drill | feature | Product databases, key volumes, Wheelhouse state; decryption keys held off-host |
| Keep 30 days of vitals history | feature | A sampler beside the on-demand read; feeds trends and alerts |
| Uptime, backup-age and disk alerts | feature | Needs the history sampler; external probe of a real redirect; channel alerts |
| Run Wheelhouse through its own release pipeline | feature | Needs a Wheelhouse image workflow and private ingress |

---

## Secrets

| Item | Type | Notes |
|---|---|---|
| Grant a product environment its vault token during deployment | feature | Mint, then write into the target settings; never displayed |
| Vault consumer in the backend SDK | feature | Startup resolution, bounded timeout, fail-closed; unblocks ForeverPin adoption |
| Scoped management credential for Wheelhouse | check | Vault-side change; replaces the shared administrator password |
| Record operator actions in an audit table | feature | Actor, operation, target; values never stored |
| Mint expiring product tokens | feature | Vault API change: mint accepts only a name today; hygiene already flags expiry |

---

## Deployments

| Item | Type | Notes |
|---|---|---|
| Browse releases older than the recent catalog | feature | The target journal already retains deployed bundles |
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

## SDK gaps found by Wheelhouse

| Item | Type | Notes |
|---|---|---|
| UI SDK `AlertModal.Action`/`Cancel` render as the corner close icon | issue | Fixed in the SDK working tree; tests pass in Chromium; needs a release and re-pin |
| UI SDK `query` entry imports optional persistence peers | issue | Consumers must install both persister packages; move persistence to a subpath |
| UI SDK `useAppQuery` has no polling interval | feature | Wheelhouse polls inside two hooks meanwhile |
| UI SDK `useAppQuery` exposes no background-fetching flag | feature | Added in the SDK working tree (`fetching`, `useRefresh`); needs a release and re-pin |
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
| Retire the single-image version-status query | issue | Replaced by the published artifact catalog |
| Split the dashboard bundle by route | issue | One 1.15 MB chunk today |
| Move the remaining panels to skeleton first loads | check | Attention, the fleet host and environment tables, and the secrets panels still show a spinner |
| Extract the vault admin client to the backend SDK | check | After `v0.3` proves it |

---

## Open decisions

| Item | Notes |
|---|---|
| Registrar | Settle before the Domains group |
| One vault per environment or one per host with namespaces | Vault docs assume one per product environment |

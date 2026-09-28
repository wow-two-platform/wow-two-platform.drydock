# Next versions — ship and adopt

*Last updated: 2026-09-29*

Where Wheelhouse stands on the version track, what the open versions still owe, which v0.3 blocks the following
Adoption version moves to the SDKs, and which Feature waves can follow it. This is analysis: a version doc is written
only when the developer opens that version. Feature waves and decision Points live in
[feature completeness](feature-completeness.md); this doc does not restate them.

## Position

| Fact | State on 2026-09-29 |
|---|---|
| Active version | v0.3 (Feature): 65 capabilities built; open are the local OAuth app and 29 verification checks |
| Behind it | v0.2 (Adoption): every build iteration done; its 2 verification checks await the developer |
| Code version | `0.3.0` in `Directory.Build.props` and `package.json`; both said `0.1.0` until today |
| Suites | Backend 208 (81 unit, 11 integration, 114 E2E, 2 migrations), frontend 74, runner 157: all green |
| Repository | `main` is 47 commits ahead of `origin`; CI has never run on GitHub; the remote is still `drydock` and public |
| Backend SDK | Pinned `10.0.40-beta`; `10.0.59-beta` is published; its repository holds 103 unpushed commits |
| UI SDK | Pinned `@wow-two-beta/ui-vue@0.0.7`, the latest published; its repository holds 73 unpushed commits, `CanvasArea` among them |
| Dependency audit | Backend: 5 packages with high and 4 with moderate advisories, all transitive; frontend runtime: none |

The version docs matched the code except for the Studio workspace, the base service map and the sidebar lines the
Vue migration replaced; v0.3 now records them.

---

## Close-out

Every item here is the developer's; none can be done from a chat.

| # | Item | Why it blocks |
|---|---|---|
| C1 | Tick v0.2's verification: suites green, live smoke | v0.2 stays open beside v0.3; one version should be active |
| C2 | Register a GitHub OAuth app for `http://localhost:18210` | The last open v0.3 build task |
| C3 | Run v0.3's 29 verification checks | A version closes only on the developer's pass |
| C4 | Push `main`, rename the remote to `wheelhouse`, make it private | CI and the release workflow have never run; `fleet.py` will hold real hosts |
| C5 | Push the UI SDK (CI releases the next `0.0.y`) and publish backend SDK `10.0.60` | The Adoption version can only re-pin published packages |

---

## Adopt — the Adoption version after v0.3

The dev cycle moves v0.3's proven blocks to the SDKs and re-pins them here. Verdicts: **adopt** (the SDK already
has it, or will once published), **extract** (build it in the SDK from Wheelhouse's code, then adopt), **keep**
(product logic), **defer** (named trigger).

### Frontend

| Block | Wheelhouse today | SDK target | Verdict |
|---|---|---|---|
| Re-pin | `@wow-two-beta/ui-vue@0.0.7` | The next `0.0.y` release | adopt: sweep its renames |
| Canvas | `presentation/common/components/canvasArea/` copy | `CanvasArea` in `presentation/layout` | adopt: delete the copy |
| Theme choice | Plain menu items with a check | `MenuRadioGroup` / `MenuRadioItem` (in source) | adopt |
| Trends | `fleet/components/TrendLine.vue` | `Sparkline` | adopt |
| Resource bars | `fleet/components/ResourceMeter.vue` | `MeterBar` | adopt |
| Refresh state | `application/common/useRefresh.ts` | A query-layer refresh flag | extract |
| Log reading | `deployments/components/ServiceLogsModal.vue` | A `LogViewer` display | extract |
| Header actions | `common/components/PageActions.vue` portal | An `AppShell` page-actions slot | extract |
| Sticky table head | `common/components/TableStyles.ts` | A `Table` sticky-header option | extract |
| Palette | Tokens in `bootstrap/index.css` | A `wheelhouse` theme in the registry | extract: TranscriptForge and ListingShelf copy it |
| Version display | `__APP_VERSION__` + profile-menu row | A service-versions composable | extract: TranscriptForge has the same |
| Browser tier | None (sweep S23) | Playwright fixture harness | extract |
| Contract drift | Hand-kept zod beside C# DTOs (S24) | Schemas from the API's OpenAPI | extract |

### Backend

| Block | Wheelhouse today | SDK target | Verdict |
|---|---|---|---|
| Re-pin | `WoW2.Sdk.Backend.Beta` `10.0.40-beta` | `10.0.59-beta` or later | adopt: 19 releases, check breaking changes |
| Advisories | SSH.NET 2023.0.0, Snappier 1.0.0, SQLitePCLRaw 2.1.11, Datadog.Trace 3.7.0, Microsoft.OpenApi 2.0.0 | The SDK's dependency graph | extract: fix in the SDK, verify after the re-pin |
| Status version | `SystemController` reads the informational version | A status endpoint helper | extract: TranscriptForge has the same |
| Migration stamp | `MigrationOptions.Version` left at the SDK default `v1.0` | The product version | adopt: set it from the build |
| Vault admin | `Infrastructure/Vaults/VaultGateway.cs` + session cache | A vault admin client in `Integrations` | extract: the backlog names it |
| Action header | Inline checks in `DeploymentsController`, `VaultsController` | A web filter or endpoint convention | extract |
| Audit | `AuditBehavior` + `IAuditedCommand` over the SDK hash chain | A mediator behavior beside `AddHashChain` | extract |
| Vitals sampler | `Infrastructure/Operations/VitalsSampler.cs` | A scheduled-job pattern | defer: a second sampler appears |
| Runner gateway | `Infrastructure/Deployments` | — | keep: deployment logic |

The Adoption version also closes sweep items S23 and S24, as [feature completeness](feature-completeness.md) planned.

---

## Ship — Feature waves after the Adoption version

The waves, their scope and their Points are in [feature completeness](feature-completeness.md#build-order). What
decides the order is which Points settle first:

| Wave | Needs | Readiness |
|---|---|---|
| Foundation remainder: product catalog, placeholder cleanup | Point 1 | Decision-light; ends four product identity sources |
| Operations: alerts, notifications, audit checkpoint, image cleanup | Point 6 | The sampler and audit trail exist; needs the channel |
| Topology: host preparation, platform services, databases, networks, backups | Topology points 5–11, 15, 23 | The largest wave; the earliest safe live point |
| Secrets: settings rendering, deploy-time tokens, SDK vault consumer | Points 2, 3, 9 | Waits on the vault consumer in the backend SDK |
| Live: control host, Tailscale, OAuth app, bootstrap | Points 7, 8 | Needs C4 first |
| Domains: inventory, DNS plan and apply, expiry | Points 4, 5; topology 25 | Registrar still open |

The fastest local-first order: Foundation remainder with Operations first (two Points), then Topology, which
makes a real host repeatable; Live follows Topology.

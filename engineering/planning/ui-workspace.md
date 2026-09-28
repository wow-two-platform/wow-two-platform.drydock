# Wheelhouse — Studio workspace and Vue migration

*Last updated: 2026-09-29*

## Status

- [x] User selected Studio workspace with Deployment Desk top navigation.
- [x] User approved full Vue migration; no temporary React exception remains.
- [x] Bootstrap, authentication, queries, API decoders, forms and all operational pages migrated.
- [x] Light/dark themes and responsive composition implemented.
- [x] Typecheck, 32 SFC compilations, 55 tests and production bundle passed.
- [x] Nine deployment browser workflow checks passed with fully intercepted fixture requests.
- [x] Thirteen workspace/product/secret/authentication browser checks passed with fully intercepted fixture requests.

## Result

The product rail selects the object being operated. The center shows its environment, verified release,
observed services, current resource readings and recent deployments. A contextual inspector retains
the selected target/service/deployment. Workspace, Deployments, Fleet, Secrets and Products share top
navigation, primary-action placement, account controls and theme selection.

Light uses warm chalk, white and forest; dark uses pine charcoal and muted mint. Only top navigation
uses restrained translucency. Operational data stays on opaque surfaces. Semantic labels supplement
status colors. The [design research](../research/design-research/design-research.md) records six references,
the layout studies, measured token contrast and the user's selection.

## Implementation

Paths below are relative to `engineering/codebase/wheelhouse.frontend-services/`.

| Area | Source | Contract |
|---|---|---|
| Bootstrap | `src/bootstrap/App.vue`, `main.ts`, `routes.ts` | Vue 3, Vue Router, lazy routes and authentication gate |
| Shell | `src/bootstrap/AppLayout.vue` | Top navigation, responsive layout and page-action outlet |
| Workspace | `src/presentation/workspace/WorkspacePage.vue` | Product rail, environment surface, contextual inspector and portfolio attention |
| Inventory | `src/application/workspace/WorkspaceInventory.ts` | Explicit registry-to-runner bindings; conflicts stay visible |
| Selection | `src/application/workspace/WorkspaceSelection.ts` | URL selection; invalid explicit links stay unavailable |
| Theme | `src/bootstrap/index.css`, `public/theme.js` | Semantic light/dark tokens and remembered first-paint scheme |
| Data/forms | `src/bootstrap/query.ts`, `src/bootstrap/form.ts`, `src/integration/` | Published SDK adapters, Result values and Zod wire decoding |
| Operational pages | `src/presentation/{deployments,fleet,secrets,products}/` | Vue implementations preserving action contracts and failure states |

Published dependencies replace the React runtime and SDK with `vue`, `vue-router`,
`@tanstack/vue-query` and `@wow-two-beta/ui-vue@0.0.7`. The former React components and persistence
dependencies are removed. `npm run typecheck` includes SFC script/template compilation;
`npm run build` produces the route-split production bundle. Node 22+ is required.

### Operational invariants

- Registry lifecycle, current runtime condition and deployment outcome remain separate facts.
- The literal `forever-pin` registry/repository binding maps to runner product `foreverpin`; no name normalization guesses identities.
- Unknown or invalid explicit URL selections cannot silently switch deployment targets.
- Service cards reflect observed containers, not invented dependency edges. CPU/memory are current samples, not retained history.
- Recent product activity comes from the newest 50 submissions; 30-day daily metrics remain portfolio-wide.
- Deployments accept approved target/release IDs, preserve confirmation on failure and follow the submitted job.
- Reconciliation requires exact typed target confirmation and the authoritative remote active rollout ID.
- Fleet remains read-only; code owns providers, targets and vaults.
- Secret writes and token minting bypass mutation cache. Plaintext is scoped to an active dialog and cleared on success, close or context change.
- Failed writes preserve input; request cancellation/generation guards prevent late responses restoring cleared values.
- Query-session and HTTP request-scope invalidation protect authentication changes from late responses.

## Verification

Commands ran in the frontend directory with bundled Node 24:

```sh
npm run typecheck
npm test
npm run build
```

| Check | Evidence |
|---|---|
| Vue types and compiler | All 32 SFCs compile |
| Inventory and selection | 32 tests pass: bindings, conflicts, unavailable reads, URL preservation and membership |
| API protocols | Nine tests pass: wire decoding, readiness envelopes, action headers, empty responses and failures |
| Fleet polling | One regression passes: in-flight snapshots survive timer ticks; multiple panels share one timer and the last unmount clears it |
| Sensitive forms | 13 lifecycle tests pass: input retention, cancellation, generation races, duplicate minting and plaintext cleanup |
| Responsive themes | Chromium at 1440, 1024 and 390 pixels, both themes; no horizontal page overflow |
| Workspace navigation | Matching service inspector, retained top-navigation context, complete refresh, scoped Fleet links and invalid explicit-product guard |
| Product forms | Failed-create input retained; repository normalized; edit slug immutable |
| Vault browser flows | Failed secret rotation retains input; success clears plaintext; enable/disable waits for writes; minted token is forgotten on close and absent from browser storage |
| Authentication browser flow | Sign-out removes the private workspace |
| Deployment workflow | Nine Chromium cases: refresh skeletons, selection reset, readiness, failed submit retention, polling, reconcile gate/failure/success and exact action headers |

Browser verification uses stateful fixtures and intercepts every API request. It proves frontend interactions
against the declared contracts, not actual SSH deployment or vault execution. Existing backend/runner
tests and live VPS release gates remain outside this frontend migration.
Both browser suites recorded zero page errors and zero unhandled API requests.

Final review corrected incomplete workspace refresh, Fleet environment links, overlapping snapshot polling
and cached-inventory error visibility. Refresh covers every displayed data source; Fleet links carry both
the explicitly mapped product and target; cached inventory stays visible alongside failed-read alerts.

The managed review runtime uses `https://localhost:5175` because another local frontend already owns
the default `5174`. The application still defaults to HTTPS `5174` and proxies `/api` to HTTPS `8210`.
The review runtime preserves the existing backend and HTTPS setup.

Screenshots and browser logs are session artifacts in
`/Users/max/.codex/visualizations/2026/09/26/01a0dd06-dd6e-7850-8f1d-addf5a64d143/`.
`vue-ui-qa.json` records the 13 general workflow checks; `deployment-workflow-qa.json` records the nine
deployment checks. `vue-workspace-{light,dark}-{1440,1024,390}.png` records the responsive compositions.
No commit or publication is part of this migration.

## Products layout follow-up

The user's Products screenshot exposed a sparse two-column card grid, distant search and a detached
inspector. Products now uses one connected frame: a compact 20rem catalogue beside a flexible detail
surface. Search and count stay together above single-column rows; product actions sit with the selected
record. Registry identifiers remain available through a disclosure. Search keeps selection inside its
visible results, so the detail surface cannot show a filtered-out product.

The production build and all 32 SFC compilations pass. Isolated Chromium checks verified both themes
at 1512, 1024 and 390 pixels, with one product and a 12-product fixture. Columns share their top edge
and divider; phone content stacks without page overflow. Edit, deletion confirmation, identifier
disclosure and filtered selection remain usable. Five grouped checks passed with no runtime errors
or unhandled API requests; no writes reached a backend. Evidence: `products-grouped-qa.json` and
`products-grouped-{dark,light}-{1512,1024,390}.png` in the session artifact directory above.

## Glass surfaces and Compose service map

The user requested restrained glassmorphism in both themes and a map derived from Compose, without
runtime call tracing. The feature keeps the current environment selection and adds Map/List views to
the Services section. Glass is applied to outer workspace surfaces and navigation; controls, popovers
and operational data retain readable surfaces. Unsupported blur and reduced-transparency preferences
receive solid fallbacks.

- [x] Theme tokens and selected glass surfaces implemented and visually verified.
- [x] Read-only topology endpoint derives an allowlisted projection from the last successful deployed release.
- [x] Interactive map distinguishes services, startup dependencies, shared networks and optional named volumes.
- [x] Loading, unavailable, empty, stale, disconnected and multiple-service states verified.
- [x] Runner, backend and frontend checks plus responsive browser verification complete.
- [x] The map pans and zooms on the shared `CanvasArea`; a local copy serves until the UI SDK re-pin.

### Map evidence boundary

The map describes the saved, validated Compose definition associated with the target's last successful release.
It does not execute Compose interpolation or return raw configuration. Environment values, labels,
commands, credentials and bind-mount paths are excluded. Logical network/volume identifiers avoid
exposing environment substitution values. Runtime container readings are a separate, timestamped overlay.
Unknown observations remain unavailable rather than healthy or zero. A changing target keeps its previous
declaration with an explicit warning. The health overlay requires matching release labels and ready states
from both the runtime reading and the target-state reading.
The runner compares deployment state before and after collecting containers, withholding release attribution
when a rollout, same-version redeploy or rollback changes that state.

`depends_on` describes declared startup dependencies; membership of the same network shows shared
connectivity configuration. Neither establishes observed service calls. Named volumes show declared
storage membership and can be hidden to keep the initial map compact. A selected service's declaration
remains inspectable when no container is observed. Deployment continues to apply the entire environment bundle.

Sources: [Docker service declarations](https://docs.docker.com/reference/compose-file/services/),
[Compose networking](https://docs.docker.com/compose/how-tos/networking/), and
[named volumes](https://docs.docker.com/reference/compose-file/volumes/).

### Verification and local preview

The runner suite passes 98 tests, including safe topology projection and changing-state vitals regressions.
The frontend suite passes 65 tests, including ten topology decoder, query, graph and selection cases.
The production build and all 33 SFC compilations pass. Authenticated deployment API tests pass 28 cases.
Isolated Chromium checks cover twelve grouped map scenarios and seven glass scenarios, using intercepted
fixture APIs rather than real writes. Both themes were checked at 1512, 1024 and 390 pixels; graph overflow
stays inside its focusable scroll region. Reduced-transparency mode removes blur and ambient gradients.
Measured minimum text contrast is 4.582:1 in light mode and 5.390:1 in dark mode.

Map browser coverage includes keyboard selection, Map/List retention, named-volume and dependency toggles,
declared-only services, mismatched-target rejection, refresh errors with cached data, environment changes,
and withholding health from older releases or targets needing reconciliation. Layout tests cover disconnected
services and dependency cycles. The captures were inspected visually in both themes and on phone width.

A read-only call against the running local rehearsal target returned `rehearsal-1`, `management` and `redirect`,
their external `platform` network, the `keys` named volume and no startup dependencies. Bind paths were excluded.
This verifies the saved-release projection through the real SSH transport; browser scenarios use fixtures.

The task-owned Vue preview runs at `https://localhost:5175`, with `WHEELHOUSE_API_PROXY=https://localhost:8212`
pointing to a separate updated Development API (`8212` HTTPS / `8213` HTTP). The existing `5174` / `8210`
runtime was preserved. The preview proxy returned `200` for system status and `401` for anonymous topology.
The optional proxy variable defaults to `https://localhost:8210` for ordinary local development.

Evidence in the session artifact directory above: `service-map-qa.json`, `glass-products-qa.json`,
`glass-contrast.json`, `service-map-{dark,light}-{1512,1024,390}.png` and `service-map-complex.png`.

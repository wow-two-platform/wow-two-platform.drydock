# Wheelhouse UI direction

*Last updated: 2026-09-26*

> Design research for Drydock, now Wheelhouse: six references, six layout studies, and the selected operational workspace.

## Status

- [x] Current product, frontend, runner contracts, and supplied screenshots reviewed.
- [x] Six external references compared; products, templates, and visual concepts distinguished.
- [x] Six interactive layout studies rendered and checked at desktop, tablet, and phone widths.
- [x] User selected Studio workspace combined with Deployment Desk's top navigation.
- [x] Light/dark semantic palette measured and specified; restrained glass limited to top navigation.
- [x] User explicitly approved the full Vue migration.
- [x] Typography and responsive light/dark composition verified at 1440, 1024 and 390 pixels.
- [x] Application UI implemented with the published Vue SDK; typecheck, SFC compilation and production build pass.
- [x] Final interaction verification recorded in the [implementation track](../../planning/ui-workspace.md).

The layout decision below was explicitly approved by the user on September 26.
The user delegated palette selection; the measured palette is specified below.
The accompanying interactive studies use illustrative records, not live operational data.

---

## Selected direction

Use **Studio workspace** as the base: a product navigator, an environment workspace,
and a contextual inspector. Combine it with **Deployment Desk's top-level navigation**.
Keep portfolio attention accessible without replacing the selected environment's work.

Ocharo's useful quality is the relationship between the selected object, the central work,
and the controls that affect it. Wheelhouse can preserve that relationship while operating
products, environments, services, releases, and deployment outcomes.

- Left: product selection, with actual environment counts and recent deployment outcomes.
- Center: selected product and environment, current release, readiness, services, and recent deployments.
- Right: selected target, service, deployment, or readiness failure and its relevant action.
- Top: Workspace, Deployments, Fleet, Secrets, and Products destinations; local-rig indicator, profile, and theme control.
- Workspace toolbar: selected product/environment identity and its contextual primary action.
- Appearance: restrained glass on navigation or floating chrome; solid surfaces behind operational data.

The service map is an optional environment view. A deployment list remains the default when
the operator is choosing a release or inspecting outcomes. A single target does not need an infinite canvas.

### Implementation boundary

The user authorized the interface rewrite. Existing authentication, product registration/edit/delete,
deployment confirmation and following, typed reconciliation, code-owned fleet, and write-only vault
administration must remain usable. This is a frontend lane; backend and runner contracts remain unchanged.

The user explicitly authorized full Vue migration on September 26. The implementation replaces React
with Vue 3 and published `@wow-two-beta/ui-vue@0.0.7`, including bootstrap, routes, queries, forms,
authentication and every operational page. Backend and runner contracts remain unchanged.

### Workspace contract

- Match registry slug `forever-pin` and repository `sulton-max/10x-venture-forever-pin` to literal runner product `foreverpin` through an explicit code-owned binding.
- Require one matching registry record. Keep conflicting or unregistered runner products visible independently.
- Keep registry lifecycle separate from runtime health, readiness, and deployment outcomes.
- Retain product and target selection in URL query parameters; changing products clears incompatible inspector selection.
- Preserve invalid explicit links as unavailable states. Do not silently select another target during loading or after an error.
- Use observed containers for service cards. Unknown readings remain unavailable; an empty list differs from unreadable data.
- Treat services as observation targets; deployment applies to the entire environment target.
- Inspect submission IDs through deployment history; reconcile using the authoritative target state's active rollout ID.
- Label activity derived from the newest 50 submissions as recent deployments. Daily 30-day statistics remain global.
- Use current CPU/memory readings as meters. No historical resource chart, request traffic, dependency edges, or logs are implied.

### Palette specification

Warm chalk and forest define the light theme. Pine charcoal and muted mint define the dark theme.
Both use the same spacing, hierarchy, semantic roles, and explicit status labels.
Glass belongs to navigation chrome; operational surfaces and text stay opaque.

| Semantic token | Light | Dark |
|---|---|---|
| Background | `#f4f4ef` | `#121917` |
| Card / popover | `#ffffff` | `#1b2420` |
| Foreground | `#202d27` | `#edf1eb` |
| Muted surface | `#eaece5` | `#29352e` |
| Muted text | `#56665d` | `#b2beb4` |
| Subtle text | `#5e6d64` | `#a1aea3` |
| Decorative border | `#dde2d8` | `#344138` |
| Essential input boundary | `#758579` | `#7b8d7f` |
| Primary | `#285c4b` | `#a6d4bd` |
| Primary foreground | `#ffffff` | `#14352a` |
| Secondary gold | `#806235` | `#d9bd8c` |
| Success | `#26704c` | `#a1d7b3` |
| Warning | `#966014` | `#eac582` |
| Destructive | `#b44745` | `#efa69c` |
| Information | `#306a83` | `#9dcce1` |
| Chart series 1–4 | `#2e735f`, `#8c693b`, `#5967a0`, `#aa5965` | `#98d1b5`, `#d9b583`, `#b7bee9`, `#e5abb5` |

WCAG relative-luminance calculations give these minimum contrast ratios across background, card,
and muted surfaces: subtle text **4.58:1 light / 5.54:1 dark**, input boundaries **3.27:1 / 3.63:1**,
and chart series **4.07:1 / 6.59:1**. Primary button foreground contrast is **7.71:1 / 8.12:1**.
All proposed semantic foreground/background pairs exceed **4.5:1**. These are opaque token-pair
measurements; translucent compositions, interaction states, and rendered content still require UI verification.

The old palette's white-on-success pair measured **3.32:1**, and subtle text on muted measured **2.56:1**.
Decorative borders intentionally remain quieter; essential controls use the stronger input token.
Status meaning must remain readable through labels/icons without relying solely on color.

Sources: [WCAG text contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html),
[non-text contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html),
[Radix semantic color roles](https://www.radix-ui.com/colors/docs/palette-composition/understanding-the-scale).

### Implementation checkpoint — September 26

Implementation began against repository HEAD `ea690759bd36cb5146ce4e0f6594c9e1b48d06d7`.
The selected workspace and top navigation are implemented in Vue, with the measured tokens in both themes.
The old sidebar, React bootstrap/components, persistence peers, skeleton shim and overlay keyframe workaround
are removed. Page routes load lazily. The application uses the existing APIs through decoded Result contracts.

- Typechecking and all 32 Vue single-file component compilations passed.
- All 55 tests passed: 32 inventory/selection, nine API protocol, one fleet polling and 13 sensitive-form lifecycle cases.
- The production Vite build passed with Node 24; the package requires Node 22 or newer.
- Isolated Chromium rendered the workspace at 1440, 1024 and 390 pixels in light/dark themes without page overflow.
- Thirteen browser checks verified selection, refresh, Fleet links, responsive themes, product create/edit, secret rotation/state, one-time token reveal and sign-out.
- Nine deployment browser checks passed, including readiness, failed submit retention, outcome polling and typed reconciliation.
- Browser tests recorded no runtime errors or unhandled API requests. All writes were intercepted fixtures, not live deployment or real vault mutation.
- Final interaction evidence and runtime handover are maintained in the [implementation track](../../planning/ui-workspace.md).

---

## Evidence and scope

- Repository: `wow-two-platform.wheelhouse`; the current `CLAUDE.md` confirms the Drydock rename.
- Research baseline: `6d1031d620d8b540448f1daed986d6d25180f9b5` plus then-uncommitted changes; this is historical evidence, not the implementation baseline.
- Sources inspected: product context, routes, page composition, sidebar, deployment/fleet/vault contracts, runner inventory.
- Screenshot 1: Wheelhouse Products registry, one Forever Pin record, lifecycle `Draft`, expanded metadata.
- Screenshot 2: Ocharo garment selection, central mannequin, contextual material inspector, local toolbar.
- The initial audit used source and supplied images; subsequent implementation checks are recorded above.
- External visuals were inspected on official pages or the original designer's post; documentation screenshots can lag their products.
- Existing unrelated edits were preserved. The initial research introduced no application dependency or source change; authorized implementation is tracked separately above.

The research baseline used React 19/Vite with `@wow-two-beta/ui`; the approved implementation uses Vue.
References below inform design rather than prescribing another template's component stack.

### What the screenshot reveals

The Products page prioritizes registration and database metadata. Its expanded row repeats
repository and lifecycle status, then adds an ID and creation date. Those facts explain the record,
but do not answer what runs, where it runs, or what needs the operator's attention.
The surrounding empty area is an opportunity for meaningful context, not a reason to add decorative metrics.

### What the source adds

Wheelhouse already implements deployment statistics, daily deployment charts, host/container vitals,
readiness checks, reconciliation, release drift, and vault hygiene. The redesign should connect these
capabilities around the selected object, not treat the product as an empty CRUD scaffold.

---

## Six references

### 1. Linear — Project overview

- Source: [Project overview](https://linear.app/docs/project-overview).
- Type: working-product reference; documentation screenshot, not a downloadable application template.
- Observed: quiet navigation, project context, local tabs, structured properties, and an optional details sidebar.
- Borrow: stable object selection and details that remain available while changing the central view.
- Wheelhouse adaptation: select Forever Pin, retain its environment, inspect a target or deployment alongside its work.
- Avoid: carrying over project-management concepts, issue priorities, or planning metadata into an operations console.
- Judgment: strongest structural reference for the primary workspace, combined with the supplied Ocharo layout.
- Study: **Studio workspace**.

### 2. Vercel — Managing deployments

- Source: [Managing deployments](https://vercel.com/docs/deployments/managing-deployments).
- Type: working-product reference; documented management flow and deployment-filter screenshot.
- Observed: project scope, deployment history, and filters for narrowing operational records.
- Borrow: explicit environment/release identity and direct movement from a deployment record into its details.
- Wheelhouse adaptation: current release beside the selected published release, readiness results, outcome history.
- Avoid: suggesting Wheelhouse builds from Git, manages CI, or supports every Vercel promotion/rollback behavior.
- Judgment: clearest day-to-day release workflow; less distinctive visually than a full studio workspace.
- Study: **Deployment desk**.

### 3. Railway — Project canvas

- Source: [Projects / Project canvas](https://docs.railway.com/projects#project-canvas).
- Type: working-product reference; official canvas screenshot and documented service-selection behavior.
- Observed: services placed within a project/environment canvas; selection exposes service configuration.
- Borrow: make the relationship between an environment and its services visible at a glance.
- Wheelhouse adaptation: target contains management and redirect services; selecting a service opens an inspector.
- Avoid: drag-to-wire infrastructure, invented network edges, or an editable topology over code-owned configuration.
- Judgment: useful optional view for multi-service diagnosis; unnecessary spatial overhead for simple release operations.
- Study: **Service map**.

### 4. Md Jasim Islam — E-commerce Admin Dashboard / Glassmorphism UI

- Source: [Original Dribbble design](https://dribbble.com/shots/26539889-E-commerce-Admin-Dashboard-Glassmorphism-UI).
- Type: Figma visual concept; no functional application or reusable code verified.
- Observed: a soft gradient palette, glass treatment on the sidebar, rounded metric surfaces, and chart hierarchy.
- Borrow: softly separated chrome and a restrained sense of depth.
- Wheelhouse adaptation: glass-inspired product rail or contextual toolbar over a calm canvas.
- Avoid: gradients beneath table text, glass on every surface, low-contrast operational states, and ecommerce metrics.
- Judgment: useful appearance reference after layout selection; it does not solve product navigation by itself.
- Study: **Glass cockpit**.

### 5. Tremor — Overview template

- Sources: [Official templates](https://blocks.tremor.so/templates), [live Support preview](https://overview.tremor.so/support).
- Type: downloadable React/Next.js template with source/download links on the official page.
- Observed: a compact metric grid, distributions, trend charts, and a detailed record table.
- Borrow: chart hierarchy, meaningful grouping, and the path from a summary to individual records.
- Wheelhouse adaptation: deployment outcomes, rollout duration, resource snapshots, and affected targets.
- Avoid: support-ticket metrics, a dashboard of unrelated KPIs, and implying host history exists because charts look plausible.
- Judgment: strongest overview/monitoring reference; use inside the product rather than making every task a dashboard.
- Study: **Health overview**.

### 6. shadcn/ui — dashboard-01

- Sources: [Official blocks / dashboard-01](https://ui.shadcn.com/blocks#dashboard-01),
  [live preview](https://ui.shadcn.com/view/new-york-v4/dashboard-01).
- Type: reusable React block; the official page supplies source and an installation command.
- Observed: inset sidebar, concise header, summary cards, interactive area chart, and data table.
- Borrow: consistent spacing, compact records, familiar filtering, and a clear boundary between navigation and work.
- Wheelhouse adaptation: deployment history, fleet inventory, and vault administration with contextual details.
- Avoid: copying sample business metrics or importing a second component system merely to obtain the layout.
- Judgment: practical implementation reference for dense pages; weaker as a distinctive overall identity.
- Study: **Operator console**; its inspector is a Wheelhouse adaptation, not a claim about the original block.

### Selection

The studies are original Wheelhouse adaptations, not replicas of these products or templates.
They emphasize different layout/interaction structures with a shared content vocabulary and restrained palette.
Glass cockpit includes a surface-treatment experiment because glass was explicitly requested;
it should not be interpreted as an approved palette or styling decision.

Prefer **Studio workspace**, borrowing Vercel's release clarity and Railway's optional service view.
Use Tremor-style summaries on Overview and dense console patterns for inventory/history.
No template purchase, installation, or framework migration is needed to select this direction.

### Study verification

- All six studies rendered at content widths `1024`, `736`, and `320` pixels without horizontal overflow.
- Browser checks verified fleet navigation, chart-day selection, readiness inspection, release-row selection, and service-node selection.
- The selected deployment row and service node open their matching inspector.
- Studio light, dark, and phone compositions were visually inspected; all six light compositions were reviewed.
- No JavaScript page errors occurred during these checks.
- These checks validate the illustrative studies only. They do not validate application integration or production state.

---

## Translate Ocharo's layout logic

| Ocharo role | Wheelhouse role | Reason |
|---|---|---|
| Garment library | Products and environments | Select the object being operated |
| Mannequin workspace | Selected environment and its services | See the object's operational state |
| Material inspector | Service, target, or deployment details | Inspect and act without losing context |
| Edit toolbar | Readiness and published-release controls | Keep actions adjacent to their target |
| Saved/export state | Environment identity and deployment outcome | Know where an action applies and what happened |

The analogous central object is the **selected environment**, not a large dashboard chart.
On a narrow screen, the inspector becomes a detail page/sheet with an explicit return to the retained selection.
Keep one authoritative product/environment context; do not stack multiple competing sidebars and pickers.

---

## Navigation that carries useful information

Navigation can answer a small operational question before the operator opens its page.
None of the six inspected references establishes an interactive chart inside a menu row;
the following is a proposed Wheelhouse adaptation.

| Item | Compact signal | Destination |
|---|---|---|
| Overview | Count of actionable issues | Attention list |
| Deployments | Recent outcomes/activity strip | History in the same product/environment scope |
| Fleet | Unreachable/unhealthy count | Affected hosts or containers |
| Secrets | Hygiene issue count | Affected vault or namespace |
| Product row | Environment health plus last deployment outcome | Product workspace |

- Keep icon, label, and destination stable; signals supplement the name.
- Treat the whole navigation row as one link; do not nest deploy/reconcile buttons inside it.
- Use a separate button if a badge applies a narrower filter than the row destination.
- A deployment strip can show recorded daily outcomes; a Fleet badge should show current health, not fictional CPU history.
- Put time range and freshness in accessible text or focus/hover detail.
- Keep errors, missing data, stale data, and zero activity visually distinct.
- Collapse to label/icon plus attention dot at narrow widths; preserve details on the destination page.

---

## Available data versus new work

| UI idea | Current support | Design boundary |
|---|---|---|
| Deployment history chart | Daily succeeded/failed/refused records | Wheelhouse submissions, not every infrastructure event |
| Deployment summary | 30-day metrics and previous-window comparisons | Preserve null when records are insufficient |
| CPU/RAM/disk gauge | Current host/container snapshots | Show sample age; these are not retained historical series |
| Attention badges | Existing derived rules for fleet, deploys, and vaults | Needs shared navigation state and precise destinations |
| Current versus selected release | Target state and approved artifact catalog | Verified current release may be unknown |
| Product/environment workspace | Data exists in separate domains | Explicit registry-to-runner identity mapping required |
| Service map | Hosts, targets, configured services, observed containers | Containment is available; dependency edges are not established |
| Uptime SLA/cost/backup dashboard | Broader planned capabilities | Do not show operational values until sourced |

### Integration prerequisite

The supplied registry record uses `forever-pin`; the runner and release catalog use `foreverpin`.
Both reference `sulton-max/10x-venture-forever-pin`, but repository matching alone is not an approved identity contract.
A product workspace needs an explicit mapping so it does not silently display an empty environment.
Keep lifecycle `Draft` separate from runtime health, deployment readiness, and last deployment outcome.

---

## Historical source anchors

Paths below are relative to this repository. They describe the pre-migration research snapshot;
the React paths were retired by the Vue migration and are retained only as audit provenance.
Current implementation anchors are listed in the [implementation track](../../planning/ui-workspace.md).

- `product/context.md:7`: essential deployment slice and local/live evidence boundary.
- `product/context.md:18`: external builds, code-owned fleet, write-only secrets.
- `engineering/codebase/wheelhouse.frontend-services/src/bootstrap/routes.tsx:20`: five top-level routes.
- `engineering/codebase/wheelhouse.frontend-services/src/bootstrap/AppLayout.tsx:50`: shared page frame.
- `engineering/codebase/wheelhouse.frontend-services/src/presentation/products/components/ProductsPanel.tsx:93`: registry details.
- `engineering/codebase/wheelhouse.frontend-services/src/presentation/common/pages/OverviewPage.tsx:16`: current panel sequence.
- `engineering/codebase/wheelhouse.frontend-services/src/presentation/shell/components/Sidebar.tsx:86`: icon/label navigation.
- `engineering/codebase/wheelhouse.frontend-services/src/presentation/deployments/components/DeploymentStatsPanel.tsx:53`: charts and metrics.
- `engineering/codebase/wheelhouse.frontend-services/src/presentation/deployments/components/DeployModal.tsx:36`: readiness and deployment flow.
- `engineering/codebase/wheelhouse.frontend-services/src/domain/overview/AttentionRules.ts:29`: coarse attention destinations.
- `engineering/codebase/wheelhouse.frontend-services/src/application/fleet/useFleetVitals.ts:6`: snapshot refresh behavior.
- `engineering/codebase/wheelhouse.runner-services/transport.py:277`: fleet snapshots.
- `engineering/codebase/wheelhouse.runner-services/transport.py:323`: recorded deployment metrics.
- `engineering/codebase/wheelhouse.runner-services/fleet.py:71`: rehearsal target/product identity.
- `engineering/codebase/wheelhouse.runner-services/artifacts.py:32`: approved product/repository identity.
- `engineering/planning/backlog.md:13`: planned retained vitals sampling.

---

## Points

- [x] Base layout: **Studio workspace** combined with **Deployment Desk's top navigation**, selected by the user.
- [x] Framework scope: full Vue migration explicitly authorized by the user.

Palette selection is delegated to this implementation. Typography and responsive details are implementation
work within the selected direction, not additional approval gates.

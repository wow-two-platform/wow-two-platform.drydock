# Design philosophy — layout, language and where to improve first

*Last updated: 2026-09-29*

What Wheelhouse should feel like as the portfolio grows from one product to fifty or more: the principles the
interface follows, why today's layout will not scale, the proposed structure, the vocabulary, and a short roadmap.
This is analysis for a planning conversation; decided points move into a version track.

## What Wheelhouse is

One control plane for a portfolio of small products: it **governs** the infrastructure (servers, vaults, domains,
access), **delivers** releases (builds, environments, promotion, rollback), and **operates** what runs (health,
logs, vitals, audit). Three jobs, one operator, many products.

---

## Principles

| # | Principle | In practice |
|---|---|---|
| P1 | Scope before action | Every screen names its scope (portfolio, product, environment, service) in the header and the URL; an action acts on that scope only. |
| P2 | Observed truth, with its source | Declared, observed and outcome stay separate facts, each with a time; unknown never renders as healthy or zero. |
| P3 | Calm until something needs you | The home surface leads with what needs action; healthy products stay quiet. |
| P4 | Stable shapes | Chrome spans the window, scrolling happens inside regions, loading keeps the layout, a refresh flashes values only. |
| P5 | Code owns the infrastructure | Servers, targets, vaults and (soon) the product catalog are defined in code; the interface reads them and runs governed actions. |
| P6 | One plain vocabulary | Product, environment, service, server, release, build, deployment, site, secret, vault. No metaphors on screen. |
| P7 | Consequential actions are deliberate | Production and destructive actions show their scope and need typed confirmation; every action lands in the audit trail. |
| P8 | Dense, not crowded | Tables and compact rows for scanning; one short line of copy at most beside an action. |
| P9 | The SDK owns every generic pattern | Shell, navigation, loading, refresh, canvas, tables, command palette come from the UI SDK; Wheelhouse composes. |
| P10 | The window is the page | A dashboard never grows past the viewport: the frame stays put, and each block and modal owns its scroll. |
| P11 | Every product is recognisable at a glance | Each product shows its own icon from its repository, else a monogram tinted by its name. |

---

## Today's layout

Top navigation: **Workspace · Deployments · Servers · Secrets · Products · Activity**. The Workspace is product-scoped
(product rail, environment surface, inspector); every other page is tool-scoped and lists the whole portfolio.

| What works | What breaks as products grow |
|---|---|
| The Workspace keeps one product's environments, services and deployments together | Two axes mix: acting on one product means leaving its Workspace for tool pages that list every product |
| Tool pages give portfolio-wide views (all deployments, all hosts, all vaults) | Those lists grow linearly with products; the product rail does too |
| Attention list on the Workspace | No portfolio home: nothing shows fifty products' health at a glance |
| Secrets page per vault and namespace | Secrets are organised by vault, not by product; finding a product's secrets means knowing its namespace |
| Service map per environment | Logs, versions and vitals for a service live on three different pages |
| Environments, services map and recent deployments stack in one column | The environment switch sits in the middle, yet it rescopes the map and the deployments below it |
| Portfolio attention sat in the product Workspace | Portfolio-wide warnings belong to the portfolio; they now open the Servers page |

---

## Proposed structure — portfolio level and product level

### Portfolio level (global)

| Destination | Holds |
|---|---|
| **Products** (home) | A gallery (or table) of every product: health per environment, version, last deploy, sites, attention badges; filter by lifecycle, search, sort by attention |
| **Servers** | Hosts, capacity against declared limits, vitals and trends, which products each host runs |
| **Vaults** | Vault health, rotation hygiene across all products, token expiry |
| **Domains** | Registrar inventory, DNS plans, certificate and domain expiry (future wave) |
| **Activity** | The audit trail across everything, filterable by product, actor and action |

### Product level (opening a product)

`/products/:product/:tool`. The environments form a fixed rail on the left of the product (`dev · test · prod`, each
with its release and health); choosing one rescopes everything to its right — the service map, recent deployments,
logs and secrets — so the scope is always visible and one click away. The product header carries the icon, name,
repository actions and an info popover for registry identifiers.

| Tool | Holds |
|---|---|
| **Overview** | The three environments side by side: release, health, sites, what a promotion would change |
| **Deployments** | This product's history, the deploy dialog, its releases and commit builds |
| **Services** | The service map and list per environment; each service's version, containers and logs in one inspector |
| **Logs** | Service output per environment (today's log tail, later streaming) |
| **Secrets** | The product's namespaces, one per environment, resolved from the vault it uses |
| **Servers** | Where each environment runs and its share of the host |
| **Activity** | The audit trail filtered to this product |
| **Settings** | Repository, catalog entry, lifecycle, costs, kill-gate metrics |

### Navigation

- Top bar (horizontal navbar): brand and version · product switcher (a searchable combobox with recent products) ·
  portfolio destinations · command palette (⌘K) · account.
- Inside a product, a second navigation for the tools. Two layout modes worth trying: tabs under the product header
  (horizontal) or a narrow rail at the left (vertical). The SDK `Navbar` now supports both orientations.
- The command palette jumps to any product, environment or service and runs scoped actions (deploy, read logs,
  rotate a secret); at fifty products it replaces most clicking.
- URLs carry the scope, so a link opens the same product, environment and tool.

The product catalog decision (feature completeness Point 1) is the data prerequisite: the gallery needs one product
identity instead of four.

---

## Vocabulary

| On screen today | Proposal | Reason |
|---|---|---|
| Fleet (page) | **Servers** — done | Matches the API (`/api/servers`) and the domain; plain |
| Workspace (page) | **Products** home + product tools | The product becomes the unit of navigation |
| Products (registry page) | Product **Settings** + "Add product" on the home | Registry editing is administration, not a destination |
| target (check, confirmation) | **environment** on screen; `target` stays in code | A target is one product environment on one server |
| Reconcile | keep, with a one-line hint | The standard term for returning to a known state |
| Local server (badge) | keep | Plain |
| Wheelhouse, ship logo | keep as the brand | A name, not a metaphor used inside; renaming costs repository, OAuth app and docs |

Internal names that still carry the old metaphors: the runner's `fleet.py` (servers, targets, vaults) and
`rehearse.py` (the local server rig). Renaming them touches the runner, its tests, the backend gateway and docs;
it fits the product catalog work (Point 1), where `fleet.py` splits into code-owned catalogs anyway.

---

## SDK standards this relies on

| Standard | UI SDK state | Wheelhouse state |
|---|---|---|
| Full-width shell, main region scrolls | `AppShell` `scroll: 'region'`, inferred navigation (in source) | Same behaviour in its own frame until the re-pin |
| Horizontal and vertical navbars, surfaces | `Navbar` `orientation`, `variant`; `NavItem` sizing (in source) | Hand-built top bar until the re-pin |
| Loading keeps shape | `SkeletonStateSlot`, `SkeletonStateGroup`, `SkeletonStateText`, `useRefresh` (in source) | Local copies; Secrets and deployment stats follow it |
| Loading button keeps its label | `Button` `isLoading` swaps only the icon (in source) | `RefreshButton` mirrors it on every page |
| Pan-and-zoom canvas | `CanvasArea` (in source) | Local copy on the service map |
| Command palette, data table, radio menus | Published or in source | Not used yet |

Every "in source" row waits on one publish of the UI SDK and one re-pin; that single step removes four local copies.

---

## Roadmap — where to start

1. **Publish and re-pin** (Adoption). Publish the UI SDK, re-pin Wheelhouse, delete the local copies, move the frame
   onto `AppShell` + `Navbar`. Every later screen then gets the standards for free.
2. **Shape-keeping first loads everywhere.** Workspace, Deployments, Servers, Products and Activity follow the
   Secrets page: frame and labels render at once, values arrive into slots.
3. **Product catalog** (Point 1). One code-owned product identity, so a gallery and product URLs are possible.
4. **Portfolio home and product workspace** (Feature). Products gallery, product tools under `/products/:product`,
   the environment switcher, the product switcher and the command palette. Tool pages become portfolio governance
   views (Servers, Vaults, Activity).
5. **Reliability waves** from [feature completeness](feature-completeness.md): alerts (Point 6), host preparation
   and backups, settings rendering, then the control host.

Steps 1 and 2 need no decision. Step 3 needs Point 1. Step 4 needs the brainstorm below.

---

## Brainstorm questions

- Gallery cards or a dense table as the default home view — or both with a toggle?
- Which facts belong on a product card: per-environment health dots, version, last deploy, attention count, cost?
- Product tools as tabs under the header, or a vertical rail?
- The environment rail rescopes every tool; does Overview still compare all three side by side?
- Do Vaults stay a portfolio page, or do secrets live only under each product?
- Lifecycle stages for grouping the portfolio: idea, building, live, paused, killed?
- Keep the Wheelhouse name?

# Polish track

*Last updated: 2026-09-25*

> The **chore / polish lane**, parallel to `versions/` — work that changes only *how* wheelhouse is built (convention alignment · SDK sync · frontend architecture), never *what* it does. Run in a **separate chat** from the version track. Not versioned; each group closes when green. Deferred/maybe work still lives in `backlog.md` — this is the *active* polish queue.

> September 19: backend checks pass. The July blocker table below is historical.
> Current deployment work and evidence: [pilot](deployment-pilot.md), [CI/artifact policy](ci-artifact-policy.md).
> The server registration form has been retired; only the product form uses the forms engine.

## 1 · Backend SDK sync — finish the (killed) Iter-11 adoption · build green; L2–L4 open

> wheelhouse bumped `10.0.37`→`10.0.40-beta`; error-layer repaired (14 handlers → `AppResult<T>` + `AppError`) + `Web.Hosting.Spa` adopted. A mid-flight agent was **killed** → 2 compile blockers + 3 leaves remain. **Fix blockers FIRST — the tree does not build.**

| # | Task | Where | Effort |
|---|---|---|---|
| **B1** 🔴 | Unit test still on old `AppResult<S,F>` → `AppResult<T>` (`.Success/.Failure` → `.Ok/.Fail`) | `Wheelhouse.Tests.Unit/Products/ProductVersionStatusQueryHandlerTests.cs:13` | 15m |
| **B2** 🔴 | `PostgresFixture` unresolved — fix the `using`/namespace for `10.0.40` | `Wheelhouse.Tests.E2E/Harness/WheelhouseAppFixture.cs:25` | 15m |
| **L2** | **TestAuth header-gate** — delete local `TestAuth.cs`, wire SDK `AddTestAuth(o => o.RequiredHeader = "X-Test-Admin")` (keep anon→401 + admin→200) | `…E2E/Harness/TestAuth.cs` + fixture + `AuthGateE2ETests` | 1–2h |
| **L3** | **Testing.Integrations fakes** — delete local `Stub*`/`Fake*`, use SDK `Fake{GitHub,ContainerRegistry}Client` | `…E2E/Harness/Stub*`, `…Unit/Fakes/Fake*` + call sites | 2–3h |
| **L4** | **EfRepository base** — `EfProduct/ServerRepository : EfRepository<X,Guid>` (inherit CRUD; keep `Exists*` + `CreatedAtUtc`/`Id` ordering) | `Wheelhouse.Persistence/Repositories/*.cs` (46/47→~10 LOC) | 1–2h |
| **V** | Verify — `dotnet test Wheelhouse.BackendServices.slnx -p:SkipSpaBuild=true` → 115 green | — | 15m |

Skip (not wheelhouse): `10.0.39` crypto (vault) · `10.0.40` qr-codegen (smart-qr).

## 2 · Frontend alignment — architecture + UI lib · ~5.5h

> wheelhouse's frontend is **flat** (`src/{api,components,hooks,lib}`); the established architecture mandates layered **`bootstrap → presentation → application → domain + integration`**, domain-sliced, barrel-exported, `@/`-aliased.

| # | Task | Type |
|---|---|---|
| ~~**F1**~~ | ~~Bump `@wow-two-beta/ui` `0.0.68`→`0.0.70` + `npm install`~~ ✅ superseded — bumped straight to **`0.0.95`** by the F-2f forms-engine proof (2026-07-10); subpaths rewritten `/actions`→`/presentation/actions` etc. `0.0.95` (not `0.0.94`) is the floor — the F-2a `Field` chrome that auto-renders field errors from context landed in `0.0.95`. | SDK |
| ~~**F2**~~ ✅ | **Layered restructure** — `src/` → `bootstrap/`·`presentation/`·`application/`·`domain/`·`integration/`, each domain-sliced (`products`·`servers`·`auth`·`common`) + `index.ts` barrels | arch |
| ~~**F3**~~ ✅ | **Enums → `const`-object-`as const`** + `*Labels` Record + `enumOptions()` — **breaking** (update every `status === 'Active'` → `=== ProductStatus.Active`) | arch |
| ~~**F4**~~ | ~~Forms → `*Values` pattern~~ ✅ done via F-2f (2026-07-10) — `RegisterProduct`/`RegisterServer` on the SDK **forms engine** (`useAppForm` via `src/form.ts` tanstack pin, `{Model}Values` + zod `{Model}Schema`, presentation `Field` chrome, resolve-on-submit) | forms |
| **F5** | Style sweep — PascalCase files / camelCase folders · JSDoc on components · `props.x` (no destructure) · 7-group import order · section dividers | style |
| ~~**F6**~~ ✅ | Config — `@/`→`src/` alias · confirm `strict` · `index.css` imports `tailwindcss` + `@wow-two-beta/ui/styles.css` + `themes.css` + `@source`s the layers | config |
| ~~**F7**~~ ✅ | **API client → SDK `/query` layer** — replace the hand-rolled `src/api/client.ts` fetch client + `useProducts`/`useServers`/`useProductVersion` effect-hooks with `createApiClient` + `useAppQuery`/`useAppMutation` (form submits then invalidate keys instead of `reload()`). Deferred out of the F-2f forms proof — only the error type was adopted (client now throws the SDK `foundation/http` `ApiError` so ProblemDetails field errors auto-land on forms) | arch |

## 3 · Convention alignment · ✅ done (2026-06-22)

- Test projects → 4-tier `Wheelhouse.Tests.{Unit·Integration·E2E·Migrations}` (split from the old 3).
- `Store` → `Repository` (interfaces + EF impls + 24 refs) per `component-names.md`.
- Was **115 green** *before* the §1 Iter-11 bump — §1 must restore that.

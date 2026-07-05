# Handoff — drydock (version + polish tracks)

*2026-06-22 · context-full restart*

## TL;DR
- **Two parallel chats on ONE drydock working tree:** *version track* (close `v0.2`) + *polish track* (`planning/polish.md` — chores). Coordinate by disjoint files; never clobber the other lane.
- **Tree is RED** — a killed mid-flight agent left drydock mid-`v0.2`-Iter-11 backend-sync. Fixing it (polish §1) unblocks everything.
- **Nothing is committed.** User manages git. Large uncommitted changeset spanning 2 lanes.

---

## State snapshot
| Thing | State |
|---|---|
| SDK backend | `WoW2.Sdk.Backend.Beta` **`10.0.40-beta`** published; the 4 leaves extracted this session are live (`Web.Hosting.Spa` · `TestAuth` header-gate · `Testing.Integrations` fakes · `EfRepository`) — all wired into the SDK `.slnx` + `publish.yml` |
| SDK frontend | `@wow-two-beta/ui` **`0.0.70`** published; drydock pins `0.0.68` |
| drydock backend | tree bumped `10.0.37`→`10.0.40`, error-layer repaired (14 handlers), Spa adopted — **build RED** (2 blockers) + 3 leaves unfinished |
| drydock tests | 4-tier `Drydock.Tests.{Unit·Integration·E2E·Migrations}` — was **115 green** *before* the Iter-11 bump |
| drydock frontend | flat `src/{api,components,hooks,lib}` — needs layered-arch restructure |
| other lane (leave intact) | uncommitted `Persistence/Migrations/003-audit-updated-at/` + `AuthSettings` + Domain `IAuditable`/`IHasTableName` |

---

## Version track — close `v0.2`
- `v0.2` = "Full SDK adoption". Iters **1–8 ✅**; **Iter 9** (build-all + suites green + live smoke) remains → then close.
- ⚠️ **Gated on polish §1** (green build) — can't verify Iter 9 while RED.
- **Next feature version = `v0.3` = the deploy slice** (SSH executor · live logs (SignalR) · deploy history + rollback — see `planning/planning.md` + `backlog.md`). The real MVP work; scope it once v0.2 closes.

---

## Polish track — `planning/polish.md`
- **§1 Backend sync 🔴** — `B1` `AppResult<S,F>`→`AppResult<T>` in `Drydock.Tests.Unit/Products/ProductVersionStatusQueryHandlerTests.cs:13` · `B2` `PostgresFixture` `using` in `Drydock.Tests.E2E/Harness/DrydockAppFixture.cs:25` · `L2` TestAuth header-gate · `L3` Integrations fakes · `L4` EfRepository base · verify 115.
- **§2 Frontend** — `F1` bump UI `0.0.70` · `F2` layered restructure (`bootstrap`/`presentation`/`application`/`domain`/`integration`) · `F3` enums-as-`const` · `F4` `*Values` forms · `F5` style · `F6` config.
- **§3 Convention** ✅ done (test 4-tier · `Store`→`Repository`).

---

## Gotchas
- **Error-layer breaking change** (`10.0.38+`): `AppResult<T,F>`→`AppResult<T>` · `DomainError`→`AppError`/`AppErrorType` · `ICategorizedFailure` deleted · result→HTTP via `AppErrorProblemDetailsFactory` / `IErrorHttpStatusCodeMapper`. Repair mostly done; the 2 blockers are stragglers.
- **SDK companion pkgs** that `ProjectReference` the core mono-lib need `<FrameworkReference Include="Microsoft.AspNetCore.App" />` or restore NU1109-downgrades.
- **Build/test:** `dotnet test <backend-services>/Drydock.slnx -p:SkipSpaBuild=true` (the `BuildSpa` target runs npm — skip it for backend-only).
- **Never** `git checkout/restore/stash/reset` to "clean up" — 2 lanes' uncommitted work lives in the tree. Found unexpected changes → assume another lane's, leave them.

---

## Key paths
- **BE** = `engineering/codebase/drydock.backend-services/` (`Drydock.slnx`, folders `services/`+`tests/`).
- **FE** = `engineering/codebase/drydock.frontend-services/`.
- Planning: `engineering/planning/{planning,backlog,polish,rules}.md` · versions: `engineering/versions/v0.2/v0.2.md`.
- SDK repo: `workbench/wow-two-sdk-beta/wow-two-sdk.backend.beta` (source to read the new APIs).
- Conventions: `wow-two-ws/conventions/` (esp. `development/backend/foundation/component-names.md`, `development/frontend/`, `development/backend/testing/testing.md`).

---

## Pick up
1. **(polish chat)** §1 blockers `B1`+`B2` → build green → leaves `L2`–`L4` → **115 green**; then §2 frontend.
2. **(version chat)** once green → Iter 9 build-all + user live-smoke → **close v0.2** → scope **`v0.3` deploy slice**.

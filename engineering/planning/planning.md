# Wheelhouse — Engineering planning

*Last updated: 2026-09-26*

> The build view: versions, phases and the document that owns each track.

## Versions

Active and past versions: [version track](version-track/version-track.md). Only the next version is planned;
deferred work waits in the [backlog](backlog.md).

---

## Phases

| Phase | Scope | State |
|---|---|---|
| Foundations | PostgreSQL, private GitHub-authenticated dashboard, product inventory | Done |
| Governed deployments | Code-owned fleet, published artifact catalog, SSH runner, health gates, independent recovery | Done locally |
| Operations and secrets | Deployment history, target checks, reconciliation, vault console, metrics dashboard | In progress (`v0.3`) |
| Hosting | VPS preparation, encrypted backups, uptime and disk alerts | Planned |
| Domains | Registrar and DNS integrations, routes, expiry inventory | Planned |
| Portfolio | Placement, more providers, data relocation, costs | Planned |

---

## Tracks

- [Studio workspace](ui-workspace.md) — Vue migration, merged navigation, themes and interaction verification.
- [Deployment pilot](deployment-pilot.md) — ForeverPin launch gates and live wiring.
- [CI and artifact policy](ci-artifact-policy.md) — release cadence, registry and catalog rules.
- [Polish](polish.md) — behavior-invariant cleanup.
- [Rules](rules.md) — operational rules.

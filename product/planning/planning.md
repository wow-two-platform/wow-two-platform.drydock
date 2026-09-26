# Wheelhouse — Product planning

*Last updated: 2026-09-19*

## Essential milestone

Select a published ForeverPin artifact and a code-owned environment; deploy with a verifiable outcome,
retain persisted codes and cookie keys, and recover independently of the control plane.
A working public redirect and editor, real provider callbacks and a verified backup close the live pilot.

| Phase | Scope | State |
|---|---|---|
| Foundations | PostgreSQL, private authenticated dashboard, products | Locally verified |
| Deployment | Code-owned fleet, published artifact catalog, SSH runner, readiness and compatible image recovery | Implemented; live wiring open |
| Domains | Explicit registrar/DNS integrations, routes and expiry inventory | Planned |
| Operations | Uptime, backup age, disk, costs and restore drills | Planned |
| Portfolio | Placement, additional provider integrations and explicit data relocation | Planned |

New VPSs and providers are added through reviewed code, not dynamic UI configuration.
Git/CI remains outside Wheelhouse's deployment controls.
Detailed work and evidence: [pilot](../../engineering/planning/deployment-pilot.md),
[CI/artifact policy](../../engineering/planning/ci-artifact-policy.md).

# Wheelhouse — Context

*Last updated: 2026-09-26*

## Current state

Wheelhouse is the private infrastructure-governance control plane. Its essential slice is deploying a published
ForeverPin release to a reviewed VPS target, with durable outcomes and recovery independent of the dashboard.
The .NET/React application uses PostgreSQL, GitHub authentication and an explicit production owner allowlist.
Products, a read-only fleet, release-artifact selection and deployment operations are implemented locally.
Operators see deployment history, check a target read-only, reconcile a locked target and administer secrets vaults.
The overview lists what needs attention from host and container vitals, 30-day deployment metrics and vault hygiene.
A local SSH rehearsal rig exercises deployments and the vault console end to end.
Live VPS wiring and hosted release publication remain open.

## Decisions

- GitHub Actions builds and publishes artifacts; Wheelhouse consumes them without controlling Git or CI.
- Main pushes validate; explicit version tags are the recommended release cut.
- Providers and individual VPS bindings are defined in code; selectable provider/environment values use enums.
- No Add VPS UI, dynamic integration registry or server mutation API.
- Runtime secrets are mounted separately. Product artifacts remain identical across environments.
- Image rollback requires schema compatibility; database recovery is an explicit operation.
- Secrets Vault stays a separate service; Wheelhouse is its central console and never reads values back.
- Domain, cost, backup and broader operations governance remain in scope for later slices.

The [CI/artifact policy](../engineering/planning/ci-artifact-policy.md) owns the detailed build and registry analysis.
The [deployment pilot](../engineering/planning/deployment-pilot.md) owns current verification and live launch gates.

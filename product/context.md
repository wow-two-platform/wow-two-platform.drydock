# Wheelhouse — Context

*Last updated: 2026-09-28*

## Current state

Wheelhouse is the private infrastructure-governance control plane. Its essential slice is deploying a published
ForeverPin release to a reviewed VPS target, with durable outcomes and recovery independent of the dashboard.
The .NET/Vue application uses PostgreSQL, GitHub authentication and an explicit production owner allowlist.
Products, a read-only fleet, release-artifact selection and deployment operations are implemented locally.
Operators see deployment history, check a target read-only, reconcile a locked target and administer secrets vaults.
The studio workspace selects a product/environment, shows observed services and release state, and opens contextual details.
Its portfolio attention list combines host/container vitals, 30-day deployment metrics and vault hygiene.
Workspace, Deployments, Fleet, Secrets, Products and Activity share top navigation and coordinated light/dark themes.
Every product runs `dev`, `test` and `prod` on one host; dev takes any commit's build, prod only a release that passed test.
Each rollout records its steps, requests its sites through the ingress and removes images old releases alone used.
Every operator action lands in a hash-chained audit trail. CI tests every push; Wheelhouse builds its own releases.
The local server (SSH target, Traefik, vault) exercises all of it end to end at `*.localhost:18080`.
Live VPS wiring, a Wheelhouse host and hosted release publication remain open.

## Decisions

- GitHub Actions builds and publishes artifacts; Wheelhouse consumes them without controlling Git or CI.
- Main pushes validate; explicit version tags are the recommended release cut.
- Providers and individual VPS bindings are defined in code; selectable provider/environment values use enums.
- No Add VPS UI, dynamic integration registry or server mutation API.
- Runtime secrets are mounted separately. Product artifacts remain identical across environments.
- Image rollback requires schema compatibility; database recovery is an explicit operation.
- Secrets Vault stays a separate service; Wheelhouse is its central console and never reads values back.
- Domain, cost, backup and broader operations governance remain in scope for later slices.
- All environments of a product share one host; Compose projects, networks, databases and hostnames separate them.
- Products describe services, builds and sites in one `deploy.yml`; a service keeps the version it last changed in.
- The audit trail records actions, never values; its chain shows edits, not truncation of the newest entries.

The [CI/artifact policy](../engineering/planning/ci-artifact-policy.md) owns the detailed build and registry analysis.
The [deployment pilot](../engineering/planning/deployment-pilot.md) owns current verification and live launch gates.

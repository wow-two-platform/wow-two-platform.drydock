# Wheelhouse.Tests.E2E

*Last updated: 2026-09-29*

> The primary tier: every user-facing flow through the real host — auth gates, products, deployments, vaults,
> audit and SPA serving.

- Prerequisites: Docker for the Testcontainers PostgreSQL; the host migrates on boot.
- Run: `dotnet test Wheelhouse.Tests.E2E`; SPA-serving tests read `Wheelhouse.Api/wwwroot`, which the backend build fills.
- Harness: the SDK `WebApiTestHost` and `PostgresFixture`; `Harness/` keeps the test-admin policy and the runner,
  vault and GitHub stubs.

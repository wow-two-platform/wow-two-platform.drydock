# Wheelhouse.Tests.Migrations

*Last updated: 2026-09-29*

> The bespoke SQL migrator over real PostgreSQL: apply, idempotency and rollback of every embedded migration.

- Prerequisites: Docker for PostgreSQL.
- Run: `dotnet test Wheelhouse.Tests.Migrations`.
- Harness: the SDK `MigratorHarness` and `MigratorPostgresFixture` over `Wheelhouse.Persistence/Migrations/NNN/*.sql`.

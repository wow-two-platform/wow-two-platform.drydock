# Wheelhouse.Tests.Integration

*Last updated: 2026-09-29*

> The EF model below HTTP: enum round-trips, repository predicates and ordering, unique-index constraints.

- Prerequisites: Docker for PostgreSQL (default); `WHEELHOUSE_TEST_DB=sqlite` runs the suite on in-memory SQLite.
- Run: `dotnet test Wheelhouse.Tests.Integration`.
- Harness: the SDK `RelationalTestDb` over `WheelhouseDbContext`; the schema comes from the model, not the migrator.

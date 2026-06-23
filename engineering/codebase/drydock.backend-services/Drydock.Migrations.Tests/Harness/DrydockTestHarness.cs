using Drydock.Persistence;
using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Testing.Data.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Testing.Data.Migrations;

namespace Drydock.Migrations.Tests.Harness;

/// <summary>
/// xUnit collection over the SDK <see cref="MigratorPostgresFixture"/> — the bespoke-migrator suite's shared container.
/// Its <c>ResetAsync</c> drops and recreates the <c>public</c> schema, so every migrator test applies from scratch
/// (apply / idempotency / rollback). The fixture's own <see cref="MigratorPostgresFixture.Name"/> is the collection name.
/// </summary>
[CollectionDefinition(Name)]
public sealed class MigratorCollection : ICollectionFixture<MigratorPostgresFixture>
{
    /// <summary>The collection name every migrator test class joins (matches <see cref="MigratorPostgresFixture.Name"/>).</summary>
    public const string Name = "postgres-migrator";
}

/// <summary>
/// The Drydock EF test database — a provider-switchable <see cref="RelationalTestDb{TContext}"/> over
/// <see cref="DrydockDbContext"/>. Schema comes from EF (<c>EnsureCreated</c> off <c>OnModelCreating</c>), not the bespoke
/// migrator, so the store / constraint / enum suites exercise the real EF model and run on Postgres or SQLite uniformly.
/// </summary>
/// <remarks>
/// Provider follows <c>TestSetupOptions.Current</c> (Postgres by default; a module initializer can flip the whole suite
/// to SQLite). <see cref="RelationalTestDb{TContext}.ResetAsync"/> clears data between tests (Postgres: Respawn truncate;
/// SQLite: recreate). <see cref="CreateContext"/> applies Drydock's snake_case naming so EF maps onto the created schema.
/// </remarks>
public sealed class DrydockTestDb : RelationalTestDb<DrydockDbContext>
{
    /// <summary>Builds a <see cref="DrydockDbContext"/> over the active test provider with Drydock's snake_case naming
    /// (the host convention); the SQLite-only <c>DateTimeOffset</c> conversion is applied inside <c>OnModelCreating</c>.</summary>
    protected override DrydockDbContext CreateContext(DbContextOptionsBuilder<DrydockDbContext> builder) =>
        new(builder.UseSnakeCaseNamingConvention().Options);
}

/// <summary>xUnit collection sharing one <see cref="DrydockTestDb"/> across the store / constraint / enum suites.</summary>
[CollectionDefinition(Name)]
public sealed class DrydockTestDbCollection : ICollectionFixture<DrydockTestDb>
{
    /// <summary>The collection name every EF-model test class joins.</summary>
    public const string Name = "drydock-test-db";
}

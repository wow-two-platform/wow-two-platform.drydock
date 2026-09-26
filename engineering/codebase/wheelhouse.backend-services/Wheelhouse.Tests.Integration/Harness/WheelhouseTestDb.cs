using Wheelhouse.Persistence;
using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Testing.Data.EntityFrameworkCore;

namespace Wheelhouse.Tests.Integration.Harness;

/// <summary>
/// The Wheelhouse EF test database — a provider-switchable <see cref="RelationalTestDb{TContext}"/> over
/// <see cref="WheelhouseDbContext"/>. Schema comes from EF (<c>EnsureCreated</c> off <c>OnModelCreating</c>), not the bespoke
/// migrator, so the store / constraint / enum suites exercise the real EF model and run on Postgres or SQLite uniformly.
/// </summary>
/// <remarks>
/// Provider follows <c>TestSetupOptions.Current</c> (Postgres by default; a module initializer can flip the whole suite
/// to SQLite). <see cref="RelationalTestDb{TContext}.ResetAsync"/> clears data between tests (Postgres: Respawn truncate;
/// SQLite: recreate). <see cref="CreateContext"/> applies Wheelhouse's snake_case naming so EF maps onto the created schema.
/// </remarks>
public sealed class WheelhouseTestDb : RelationalTestDb<WheelhouseDbContext>
{
    /// <summary>Builds a <see cref="WheelhouseDbContext"/> over the active test provider with Wheelhouse's snake_case naming
    /// (the host convention); the SQLite-only <c>DateTimeOffset</c> conversion is applied inside <c>OnModelCreating</c>.</summary>
    protected override WheelhouseDbContext CreateContext(DbContextOptionsBuilder<WheelhouseDbContext> builder) =>
        new(builder.UseSnakeCaseNamingConvention().Options);
}

/// <summary>xUnit collection sharing one <see cref="WheelhouseTestDb"/> across the store / constraint / enum suites.</summary>
[CollectionDefinition(Name)]
public sealed class WheelhouseTestDbCollection : ICollectionFixture<WheelhouseTestDb>
{
    /// <summary>The collection name every EF-model test class joins.</summary>
    public const string Name = "wheelhouse-test-db";
}

using AwesomeAssertions;
using Wheelhouse.Tests.Migrations.Harness;
using Wheelhouse.Persistence;
using WoW.Two.Sdk.Backend.Beta.Foundation.Results;
using WoW.Two.Sdk.Backend.Beta.Testing.Data.Migrations;

namespace Wheelhouse.Tests.Migrations.Tests;

/// <summary>
/// The marquee: the real embedded wheelhouse migrator over a fresh Postgres — what E2E can't isolate. A fresh DB,
/// migrated, yields every control-plane table plus the <c>002-snake-enums</c> row, history is recorded, a re-run is a
/// no-op (idempotent), and (with rollback enabled) the latest migration rolls back to pending. Runs on the SDK
/// <see cref="MigratorHarness"/> over the drop-schema <see cref="MigratorPostgresFixture"/>.
/// </summary>
[Collection(MigratorCollection.Name)]
public sealed class ApplyTests(MigratorPostgresFixture fixture)
{
    /// <summary>The five tables the 001-baseline migration creates.</summary>
    private static readonly string[] SchemaTables = ["servers", "products", "deployments", "domains", "secrets"];

    /// <summary>Builds a migrator over the shared container DB reading the real embedded wheelhouse migrations (001-baseline through 006-product-catalog).</summary>
    private MigratorHarness CreateMigrator(Action<WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke.MigrationOptions>? configure = null) =>
        MigratorHarness.CreatePostgres(fixture.ConnectionString, typeof(WheelhouseDbContext).Assembly, configure);

    [Fact]
    public async Task ApplyPending_OnFreshDb_CreatesSchema_RecordsHistory_AndIsIdempotent()
    {
        // A truly fresh DB — drop everything the prior test left so this asserts a from-nothing apply.
        await fixture.ResetAsync();

        await using var migrator = CreateMigrator();

        // First apply runs all six real migrations in order.
        var applied = Unwrap(await migrator.Runner.ApplyPendingAsync("startup", CancellationToken.None));
        applied.Should().Equal("001-baseline", "002-snake-enums", "003-audit-updated-at", "004-audit-trail", "005-vitals-samples",
            "006-product-catalog");

        // Every control-plane table the baseline declares now exists.
        foreach (var table in SchemaTables)
            (await migrator.HasTableAsync(table)).Should().BeTrue($"{table} should be created by 001-baseline");
        (await migrator.HasTableAsync("audit_entries")).Should().BeTrue("004-audit-trail creates the audit trail");
        (await migrator.HasTableAsync("vitals_samples")).Should().BeTrue("005-vitals-samples creates the readings table");
        (await migrator.HasTableAsync("product_metadata")).Should().BeTrue("006-product-catalog records lifecycles");
        (await migrator.HasTableAsync("integration_keys")).Should().BeTrue("006-product-catalog keeps integration keys");
        (await migrator.HasIndexAsync("ix_integration_keys_hash")).Should().BeTrue();

        // The migrator's own bookkeeping table exists, with one row per migration stamped by the host label.
        (await migrator.HasTableAsync("migration_history")).Should().BeTrue();
        var history = await migrator.ReadHistoryAsync();
        history.Select(r => r.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6);
        history.Select(r => r.Name).Should().Equal(
            "baseline", "snake-enums", "audit-updated-at", "audit-trail", "vitals-samples", "product-catalog");
        history.Should().OnlyContain(r => r.AppliedBy == "startup");
        history.Should().OnlyContain(r => r.Checksum.Length == 64); // SHA-256 hex digest recorded at apply time.

        // GetStatus: all applied, nothing pending / drifted / orphaned.
        var status = Unwrap(await migrator.Runner.GetStatusAsync(CancellationToken.None));
        status.Applied.Select(a => a.Ordinal).Should().Equal(1, 2, 3, 4, 5, 6);
        status.Pending.Should().BeEmpty();
        status.Drifted.Should().BeEmpty();
        status.Orphaned.Should().BeEmpty();

        // Second apply against an up-to-date DB is a no-op: no labels returned, history unchanged.
        var second = Unwrap(await migrator.Runner.ApplyPendingAsync("startup", CancellationToken.None));
        second.Should().BeEmpty();
        (await migrator.ReadHistoryAsync()).Should().HaveCount(6);
    }

    [Fact]
    public async Task Rollback_WithAllowRollback_RemovesLatestHistoryRow_AndReturnsItToPending()
    {
        await fixture.ResetAsync();

        await using var migrator = CreateMigrator(o => o.AllowRollback = true);
        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));

        // Roll back the latest migration only (006-product-catalog).
        Unwrap(await migrator.Runner.RollbackAsync(targetOrdinal: null, CancellationToken.None));

        // 006's history row and tables are gone; 001-005 are untouched and the earlier tables remain.
        (await migrator.ReadHistoryAsync()).Select(h => h.Ordinal).Should().Equal(1, 2, 3, 4, 5);
        (await migrator.HasTableAsync("vitals_samples")).Should().BeTrue();
        (await migrator.HasTableAsync("products")).Should().BeTrue("the legacy registry is never touched");
        (await migrator.HasTableAsync("product_metadata")).Should().BeFalse();
        (await migrator.HasTableAsync("integration_keys")).Should().BeFalse();

        // 006 is pending again — rollback returned it to the source-but-not-applied state.
        var status = Unwrap(await migrator.Runner.GetStatusAsync(CancellationToken.None));
        status.Applied.Select(a => a.Ordinal).Should().Equal(1, 2, 3, 4, 5);
        status.Pending.Select(p => p.Ordinal).Should().Equal(6);
    }

    [Fact]
    public async Task ProductCatalog_ShouldSeedEachRegisteredProductsLifecycle_WhenApplied()
    {
        await fixture.ResetAsync();
        await using var migrator = CreateMigrator(o => o.AllowRollback = true);
        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));
        Unwrap(await migrator.Runner.RollbackAsync(targetOrdinal: null, CancellationToken.None));

        await using var connection = await migrator.OpenConnectionAsync();
        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText =
                "INSERT INTO products (id, slug, name, repo, status, created_at_utc, updated_at_utc) VALUES " +
                "(gen_random_uuid(), 'pilot', 'Pilot', 'owner/pilot', 'active', now(), now()), " +
                "(gen_random_uuid(), 'sketch', 'Sketch', 'owner/sketch', 'draft', now(), now())";
            await insert.ExecuteNonQueryAsync();
        }

        Unwrap(await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None));

        await using var read = connection.CreateCommand();
        read.CommandText = "SELECT slug, lifecycle FROM product_metadata ORDER BY slug";
        var seeded = new List<(string, string)>();
        await using (var reader = await read.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                seeded.Add((reader.GetString(0), reader.GetString(1)));
        seeded.Should().Equal(("pilot", "live"), ("sketch", "building"));
    }

    /// <summary>Returns a migrator result's value, failing the test with the migrator's own message otherwise.</summary>
    private static T Unwrap<T>(Result<T> result) where T : notnull =>
        result is Result<T>.Success success
            ? success.Value
            : throw new Xunit.Sdk.XunitException(((Result<T>.Failure)result).Error.Message);
}

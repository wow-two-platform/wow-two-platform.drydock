using AwesomeAssertions;
using Drydock.Migrations.Tests.Harness;
using Drydock.Persistence;
using WoW.Two.Sdk.Backend.Beta.Testing.Data.Migrations;

namespace Drydock.Migrations.Tests.Tests;

/// <summary>
/// The marquee: the real embedded drydock migrator over a fresh Postgres — what E2E can't isolate. A fresh DB,
/// migrated, yields every control-plane table plus the <c>002-snake-enums</c> row, history is recorded, a re-run is a
/// no-op (idempotent), and (with rollback enabled) the latest migration rolls back to pending. Runs on the SDK
/// <see cref="MigratorHarness"/> over the drop-schema <see cref="MigratorPostgresFixture"/>.
/// </summary>
[Collection(MigratorCollection.Name)]
public sealed class ApplyTests(MigratorPostgresFixture fixture)
{
    /// <summary>The five tables the 001-baseline migration creates.</summary>
    private static readonly string[] SchemaTables = ["servers", "products", "deployments", "domains", "secrets"];

    /// <summary>Builds a migrator over the shared container DB reading the real embedded drydock migrations (001-baseline, 002-snake-enums).</summary>
    private MigratorHarness CreateMigrator(Action<WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke.MigrationOptions>? configure = null) =>
        MigratorHarness.CreatePostgres(fixture.ConnectionString, typeof(DrydockDbContext).Assembly, configure);

    [Fact]
    public async Task ApplyPending_OnFreshDb_CreatesSchema_RecordsHistory_AndIsIdempotent()
    {
        // A truly fresh DB — drop everything the prior test left so this asserts a from-nothing apply.
        await fixture.ResetAsync();

        await using var migrator = CreateMigrator();

        // First apply runs both real migrations in order.
        var applied = await migrator.Runner.ApplyPendingAsync("startup", CancellationToken.None);
        applied.Should().Equal("001-baseline", "002-snake-enums");

        // Every control-plane table the baseline declares now exists.
        foreach (var table in SchemaTables)
            (await migrator.HasTableAsync(table)).Should().BeTrue($"{table} should be created by 001-baseline");

        // The migrator's own bookkeeping table exists, with one row per migration stamped by the host label.
        (await migrator.HasTableAsync("migration_history")).Should().BeTrue();
        var history = await migrator.ReadHistoryAsync();
        history.Select(r => r.Ordinal).Should().Equal(1, 2);
        history.Select(r => r.Name).Should().Equal("baseline", "snake-enums");
        history.Should().OnlyContain(r => r.AppliedBy == "startup");
        history.Should().OnlyContain(r => r.Checksum.Length == 64); // SHA-256 hex digest recorded at apply time.

        // GetStatus: both applied, nothing pending / drifted / orphaned.
        var status = await migrator.Runner.GetStatusAsync(CancellationToken.None);
        status.Applied.Select(a => a.Ordinal).Should().Equal(1, 2);
        status.Pending.Should().BeEmpty();
        status.Drifted.Should().BeEmpty();
        status.Orphaned.Should().BeEmpty();

        // Second apply against an up-to-date DB is a no-op: no labels returned, history unchanged.
        var second = await migrator.Runner.ApplyPendingAsync("startup", CancellationToken.None);
        second.Should().BeEmpty();
        (await migrator.ReadHistoryAsync()).Should().HaveCount(2);
    }

    [Fact]
    public async Task Rollback_WithAllowRollback_RemovesLatestHistoryRow_AndReturnsItToPending()
    {
        await fixture.ResetAsync();

        await using var migrator = CreateMigrator(o => o.AllowRollback = true);
        await migrator.Runner.ApplyPendingAsync("test", CancellationToken.None);

        // Roll back the latest migration only (002-snake-enums).
        await migrator.Runner.RollbackAsync(targetOrdinal: null, CancellationToken.None);

        // 002's history row is gone; 001 is untouched and its tables remain.
        (await migrator.ReadHistoryAsync()).Select(h => h.Ordinal).Should().Equal(1);
        (await migrator.HasTableAsync("servers")).Should().BeTrue();

        // 002 is pending again — rollback returned it to the source-but-not-applied state.
        var status = await migrator.Runner.GetStatusAsync(CancellationToken.None);
        status.Applied.Select(a => a.Ordinal).Should().Equal(1);
        status.Pending.Select(p => p.Ordinal).Should().Equal(2);
    }
}

using AwesomeAssertions;
using Dapper;
using Wheelhouse.Domain.Deployments.Entities;
using Wheelhouse.Domain.Deployments.Enums;
using Wheelhouse.Domain.Products.Entities;
using Wheelhouse.Domain.Products.Enums;
using Wheelhouse.Domain.Servers.Entities;
using Wheelhouse.Domain.Servers.Enums;
using Wheelhouse.Tests.Integration.Harness;
using Microsoft.EntityFrameworkCore;

namespace Wheelhouse.Tests.Integration.Tests;

/// <summary>
/// The EF ↔ schema enum contract: the SDK <c>EnumCaseConverter</c> (wired model-wide by <c>ApplyEnumStringConversions</c>)
/// stores each enum as snake_case <c>text</c>, and EF reads it back to the right member. Asserts the on-disk text directly
/// (raw SQL over the context's own connection — provider-agnostic), including the multi-word <c>RolledBack ↔ rolled_back</c>
/// case that single-word casing would miss. Runs on the SDK <see cref="WheelhouseTestDb"/> (Postgres container or SQLite).
/// </summary>
[Collection(WheelhouseTestDbCollection.Name)]
public sealed class EnumRoundTripTests(WheelhouseTestDb db) : IAsyncLifetime
{
    /// <inheritdoc />
    public async Task InitializeAsync() => await db.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SingleWordEnum_RoundTrips_AndIsStoredSnakeCaseText()
    {
        var id = Guid.NewGuid();

        await using (var ctx = db.NewContext())
        {
            ctx.Servers.Add(new Server
            {
                Id = id,
                Name = "hel1-prod",
                Host = "10.0.0.1",
                SshUser = "deploy",
                Status = ServerStatus.Unreachable, // single word, but the converter still lowercases it.
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        // On disk it is plain lowercase text, not the PascalCase member name.
        (await ReadScalarAsync<string>("select status from servers where id = @id", id))
            .Should().Be("unreachable");

        // EF reads the text back to the right member.
        await using var read = db.NewContext();
        var loaded = await read.Servers.FindAsync(id);
        loaded!.Status.Should().Be(ServerStatus.Unreachable);
    }

    [Fact]
    public async Task MultiWordEnum_RolledBack_IsStoredAsSnakeCase_AndRoundTrips()
    {
        var id = Guid.NewGuid();

        await using (var ctx = db.NewContext())
        {
            ctx.Deployments.Add(new Deployment
            {
                Id = id,
                ProductId = Guid.NewGuid(),
                ServerId = Guid.NewGuid(),
                Status = DeploymentStatus.RolledBack, // the multi-word case the 002 migration + converter exist for.
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        // The whole point: RolledBack persists as rolled_back, not "RolledBack" / "rolledback".
        (await ReadScalarAsync<string>("select status from deployments where id = @id", id))
            .Should().Be("rolled_back");

        await using var read = db.NewContext();
        var loaded = await read.Deployments.FindAsync(id);
        loaded!.Status.Should().Be(DeploymentStatus.RolledBack);
    }

    [Fact]
    public async Task ProductLifecycle_ShouldBeStoredAsSnakeCaseTextAndReadBack_WhenRecorded()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.ProductMetadata.Add(new ProductMetadataEntity { Id = "smart-qr", Lifecycle = ProductLifecycle.Live });
            await ctx.SaveChangesAsync();
        }

        (await ReadScalarAsync<string>("select lifecycle from product_metadata where slug = @id", "smart-qr"))
            .Should().Be("live");

        await using var read = db.NewContext();
        (await read.ProductMetadata.FindAsync("smart-qr"))!.Lifecycle.Should().Be(ProductLifecycle.Live);
    }

    /// <summary>Reads a single scalar via the context's own ADO connection (bypasses EF; provider-agnostic — asserts the raw column on Postgres or SQLite).</summary>
    private async Task<T?> ReadScalarAsync<T>(string sql, object id)
    {
        await using var ctx = db.NewContext();
        var conn = ctx.Database.GetDbConnection();
        return await conn.ExecuteScalarAsync<T>(sql, new { id });
    }
}

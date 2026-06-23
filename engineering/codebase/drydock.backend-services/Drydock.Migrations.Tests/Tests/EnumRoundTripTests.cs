using AwesomeAssertions;
using Dapper;
using Drydock.Domain.Deployments.Entities;
using Drydock.Domain.Deployments.Enums;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Products.Enums;
using Drydock.Domain.Servers.Entities;
using Drydock.Domain.Servers.Enums;
using Drydock.Migrations.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace Drydock.Migrations.Tests.Tests;

/// <summary>
/// The EF ↔ schema enum contract: the SDK <c>EnumCaseConverter</c> (wired model-wide by <c>ApplyEnumStringConversions</c>)
/// stores each enum as snake_case <c>text</c>, and EF reads it back to the right member. Asserts the on-disk text directly
/// (raw SQL over the context's own connection — provider-agnostic), including the multi-word <c>RolledBack ↔ rolled_back</c>
/// case that single-word casing would miss. Runs on the SDK <see cref="DrydockTestDb"/> (Postgres container or SQLite).
/// </summary>
[Collection(DrydockTestDbCollection.Name)]
public sealed class EnumRoundTripTests(DrydockTestDb db) : IAsyncLifetime
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
                CreatedAtUtc = DateTimeOffset.UtcNow,
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
                CreatedAtUtc = DateTimeOffset.UtcNow,
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
    public async Task ProductStatus_DefaultDraft_IsStoredSnakeCaseText_AndRoundTrips()
    {
        var id = Guid.NewGuid();

        await using (var ctx = db.NewContext())
        {
            ctx.Products.Add(new Product
            {
                Id = id,
                Slug = "smart-qr",
                Name = "Smart QR",
                Repo = "wow-two-platform/wow-two-platform.smart-qr",
                Status = ProductStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        (await ReadScalarAsync<string>("select status from products where id = @id", id))
            .Should().Be("active");

        await using var read = db.NewContext();
        (await read.Products.FindAsync(id))!.Status.Should().Be(ProductStatus.Active);
    }

    /// <summary>Reads a single scalar via the context's own ADO connection (bypasses EF; provider-agnostic — asserts the raw column on Postgres or SQLite).</summary>
    private async Task<T?> ReadScalarAsync<T>(string sql, Guid id)
    {
        await using var ctx = db.NewContext();
        var conn = ctx.Database.GetDbConnection();
        return await conn.ExecuteScalarAsync<T>(sql, new { id });
    }
}

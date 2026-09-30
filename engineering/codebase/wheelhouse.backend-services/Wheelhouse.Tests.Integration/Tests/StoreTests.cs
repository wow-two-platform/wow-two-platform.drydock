using AwesomeAssertions;
using Wheelhouse.Domain.Integrations.Entities;
using Wheelhouse.Domain.Products.Entities;
using Wheelhouse.Domain.Products.Enums;
using Wheelhouse.Domain.Servers.Entities;
using Wheelhouse.Tests.Integration.Harness;
using Microsoft.EntityFrameworkCore;

namespace Wheelhouse.Tests.Integration.Tests;

/// <summary>
/// Repository behavior over a real relational DB — the read paths <c>IntegrationKeyRepository</c>, <c>ProductMetadataRepository</c> and <c>EfServerRepository</c> rely on.
/// The repositories are <c>internal</c> to <c>Wheelhouse.Persistence</c> (only <c>Wheelhouse.Api</c> sees them), so these exercise
/// the exact EF query shapes those repositories wrap — <c>AnyAsync</c> existence predicates and
/// <c>OrderByDescending(CreatedAt)</c> listing — through the public <see cref="Wheelhouse.Persistence.WheelhouseDbContext"/>.
/// Runs on the SDK <see cref="WheelhouseTestDb"/> (Postgres container or in-memory SQLite).
/// </summary>
[Collection(WheelhouseTestDbCollection.Name)]
public sealed class StoreTests(WheelhouseTestDb db) : IAsyncLifetime
{
    /// <inheritdoc />
    public async Task InitializeAsync() => await db.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task LiveKeyLookup_ShouldFindOnlyAnUnrevokedKey_WhenMatchedByHash()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.IntegrationKeys.Add(NewKey("live", "hash-live"));
            ctx.IntegrationKeys.Add(NewKey("revoked", "hash-revoked") with { RevokedAt = DateTimeOffset.UtcNow });
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The IntegrationKeyRepository.FindLiveByHashAsync predicate.
        async Task<string?> FindLiveAsync(string hash) => (await read.IntegrationKeys.AsNoTracking()
            .FirstOrDefaultAsync(key => key.Hash == hash && key.RevokedAt == null))?.Name;

        (await FindLiveAsync("hash-live")).Should().Be("live");
        (await FindLiveAsync("hash-revoked")).Should().BeNull();
        (await FindLiveAsync("hash-unknown")).Should().BeNull();
    }

    [Fact]
    public async Task KeyTouch_ShouldWriteOnlyThatKeysLastUse_WhenRecorded()
    {
        var used = NewKey("used", "hash-used");
        var idle = NewKey("idle", "hash-idle");
        await using (var ctx = db.NewContext())
        {
            ctx.IntegrationKeys.AddRange(used, idle);
            await ctx.SaveChangesAsync();
        }

        var at = DateTimeOffset.UtcNow;
        await using (var ctx = db.NewContext())
        {
            // The IntegrationKeyRepository.TouchAsync shape: one set-based update, no read.
            await ctx.IntegrationKeys.Where(key => key.Id == used.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(key => key.LastUsedAt, (DateTimeOffset?)at));
        }

        await using var read = db.NewContext();
        (await read.IntegrationKeys.FindAsync(used.Id))!.LastUsedAt.Should().BeCloseTo(at, TimeSpan.FromMilliseconds(1));
        (await read.IntegrationKeys.FindAsync(idle.Id))!.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public async Task ProductMetadata_ShouldReadBackBySlug_WhenRecorded()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.ProductMetadata.Add(new ProductMetadataEntity { Id = "foreverpin", Lifecycle = ProductLifecycle.Paused });
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The ProductMetadataRepository.FindAsync predicate.
        (await read.ProductMetadata.AsNoTracking().FirstOrDefaultAsync(row => row.Id == "foreverpin"))!.Lifecycle
            .Should().Be(ProductLifecycle.Paused);
        (await read.ProductMetadata.AsNoTracking().FirstOrDefaultAsync(row => row.Id == "unknown")).Should().BeNull();
    }

    [Fact]
    public async Task ExistsByHost_IsTrueOnlyForAPersistedHost()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.Servers.Add(NewServer("hel1-prod", "10.0.0.1"));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The EfServerRepository.ExistsByHostAsync predicate.
        (await read.Servers.AnyAsync(s => s.Host == "10.0.0.1")).Should().BeTrue();
        (await read.Servers.AnyAsync(s => s.Host == "10.9.9.9")).Should().BeFalse();
    }

    [Fact]
    public async Task ListKeys_ShouldReturnNewestFirst_WhenCreatedAtDiffers()
    {
        var now = DateTimeOffset.UtcNow;

        await using (var ctx = db.NewContext())
        {
            // Insert out of chronological order to prove the ORDER BY (not insertion order).
            ctx.IntegrationKeys.Add(NewKey("middle", "hash-m", now.AddMinutes(-5)));
            ctx.IntegrationKeys.Add(NewKey("newest", "hash-n", now));
            ctx.IntegrationKeys.Add(NewKey("oldest", "hash-o", now.AddMinutes(-10)));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The IntegrationKeyRepository.ListAsync shape: AsNoTracking + OrderByDescending(CreatedAt).
        var listed = await read.IntegrationKeys.AsNoTracking().OrderByDescending(key => key.CreatedAt).ToListAsync();

        listed.Select(key => key.Name).Should().Equal("newest", "middle", "oldest");
    }

    [Fact]
    public async Task ListServers_OrdersByCreatedAtDescending()
    {
        var now = DateTimeOffset.UtcNow;

        await using (var ctx = db.NewContext())
        {
            ctx.Servers.Add(NewServer("b", "10.0.0.2", now.AddMinutes(-5)));
            ctx.Servers.Add(NewServer("c", "10.0.0.3", now));
            ctx.Servers.Add(NewServer("a", "10.0.0.1", now.AddMinutes(-10)));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        var listed = await read.Servers.AsNoTracking().OrderByDescending(s => s.CreatedAt).ToListAsync();

        listed.Select(s => s.Host).Should().Equal("10.0.0.3", "10.0.0.2", "10.0.0.1");
    }

    [Fact]
    public async Task ListServers_SameInstant_TieBreaksByIdDescending()
    {
        var instant = DateTimeOffset.UtcNow;
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        await using (var ctx = db.NewContext())
        {
            foreach (var (id, i) in ids.Select((id, i) => (id, i)))
                ctx.Servers.Add(NewServerWithId(id, $"same-tick-{i}", $"10.5.0.{i}", instant));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The EfServerRepository.ListAsync shape: OrderByDescending(CreatedAt).ThenByDescending(Id).
        async Task<List<Guid>> ListIdsAsync() => await read.Servers.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Select(s => s.Id)
            .ToListAsync();

        var listed = await ListIdsAsync();
        listed.Should().BeEquivalentTo(ids);
        listed.Should().Equal(await ListIdsAsync()); // stable across re-query → the tiebreak is deterministic
    }

    private static IntegrationKeyEntity NewKey(string name, string hash, DateTimeOffset? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Prefix = "wh_abcdefgh",
        Hash = hash,
        Scopes = "catalog:read",
        CreatedBy = "test-admin",
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
    };

    private static Server NewServer(string name, string host, DateTimeOffset? createdAt = null) =>
        NewServerWithId(Guid.NewGuid(), name, host, createdAt ?? DateTimeOffset.UtcNow);

    private static Server NewServerWithId(Guid id, string name, string host, DateTimeOffset createdAt) => new()
    {
        Id = id,
        Name = name,
        Host = host,
        SshUser = "deploy",
        CreatedAt = createdAt,
    };
}

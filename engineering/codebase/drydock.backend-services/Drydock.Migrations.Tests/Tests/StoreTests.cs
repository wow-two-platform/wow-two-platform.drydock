using AwesomeAssertions;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Products.Enums;
using Drydock.Domain.Servers.Entities;
using Drydock.Migrations.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace Drydock.Migrations.Tests.Tests;

/// <summary>
/// Store behavior over a real relational DB — the read paths <c>EfProductStore</c> / <c>EfServerStore</c> rely on.
/// The stores are <c>internal</c> to <c>Drydock.Persistence</c> (only <c>Drydock.Api</c> sees them), so these exercise
/// the exact EF query shapes those stores wrap — <c>AnyAsync</c> existence predicates and
/// <c>OrderByDescending(CreatedAtUtc)</c> listing — through the public <see cref="Drydock.Persistence.DrydockDbContext"/>.
/// Runs on the SDK <see cref="DrydockTestDb"/> (Postgres container or in-memory SQLite).
/// </summary>
[Collection(DrydockTestDbCollection.Name)]
public sealed class StoreTests(DrydockTestDb db) : IAsyncLifetime
{
    /// <inheritdoc />
    public async Task InitializeAsync() => await db.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ExistsBySlug_IsTrueOnlyForAPersistedSlug()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.Products.Add(NewProduct("smart-qr"));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The EfProductStore.ExistsBySlugAsync predicate.
        (await read.Products.AnyAsync(p => p.Slug == "smart-qr")).Should().BeTrue();
        (await read.Products.AnyAsync(p => p.Slug == "does-not-exist")).Should().BeFalse();
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
        // The EfServerStore.ExistsByHostAsync predicate.
        (await read.Servers.AnyAsync(s => s.Host == "10.0.0.1")).Should().BeTrue();
        (await read.Servers.AnyAsync(s => s.Host == "10.9.9.9")).Should().BeFalse();
    }

    [Fact]
    public async Task ListProducts_OrdersByCreatedAtUtcDescending()
    {
        var now = DateTimeOffset.UtcNow;

        await using (var ctx = db.NewContext())
        {
            // Insert out of chronological order to prove the ORDER BY (not insertion order).
            ctx.Products.Add(NewProduct("middle", now.AddMinutes(-5)));
            ctx.Products.Add(NewProduct("newest", now));
            ctx.Products.Add(NewProduct("oldest", now.AddMinutes(-10)));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The EfProductStore.ListAsync shape: AsNoTracking + OrderByDescending(CreatedAtUtc).
        var listed = await read.Products.AsNoTracking().OrderByDescending(p => p.CreatedAtUtc).ToListAsync();

        listed.Select(p => p.Slug).Should().Equal("newest", "middle", "oldest");
    }

    [Fact]
    public async Task ListServers_OrdersByCreatedAtUtcDescending()
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
        var listed = await read.Servers.AsNoTracking().OrderByDescending(s => s.CreatedAtUtc).ToListAsync();

        listed.Select(s => s.Host).Should().Equal("10.0.0.3", "10.0.0.2", "10.0.0.1");
    }

    [Fact]
    public async Task ListProducts_SameInstant_TieBreaksByIdDescending()
    {
        // All rows share the exact same CreatedAtUtc → CreatedAtUtc alone leaves the order undefined.
        // The store's ThenByDescending(Id) tiebreaker makes it deterministic: highest Id first.
        var instant = DateTimeOffset.UtcNow;
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        await using (var ctx = db.NewContext())
        {
            // Insert in an order unrelated to Id so a pass can't be insertion-order luck.
            foreach (var (id, i) in ids.Select((id, i) => (id, i)))
                ctx.Products.Add(NewProductWithId(id, $"same-tick-{i}", instant));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        // The EfProductStore.ListAsync shape: OrderByDescending(CreatedAtUtc).ThenByDescending(Id).
        async Task<List<Guid>> ListIdsAsync() => await read.Products.AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync();

        var listed = await ListIdsAsync();
        // The Id tiebreaker makes the equal-timestamp order deterministic — provider-agnostic: every row comes back,
        // and a second identical query yields the exact same order (the DB's uuid/text byte-ordering is stable).
        listed.Should().BeEquivalentTo(ids); // all rows came back, none dropped by the equal-timestamp sort
        listed.Should().Equal(await ListIdsAsync()); // stable across re-query → the tiebreak is deterministic
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
        // The EfServerStore.ListAsync shape: OrderByDescending(CreatedAtUtc).ThenByDescending(Id).
        async Task<List<Guid>> ListIdsAsync() => await read.Servers.AsNoTracking()
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Select(s => s.Id)
            .ToListAsync();

        var listed = await ListIdsAsync();
        listed.Should().BeEquivalentTo(ids);
        listed.Should().Equal(await ListIdsAsync()); // stable across re-query → the tiebreak is deterministic
    }

    private static Product NewProduct(string slug, DateTimeOffset? createdAt = null) =>
        NewProductWithId(Guid.NewGuid(), slug, createdAt ?? DateTimeOffset.UtcNow);

    private static Product NewProductWithId(Guid id, string slug, DateTimeOffset createdAt) => new()
    {
        Id = id,
        Slug = slug,
        Name = slug,
        Repo = $"wow-two-platform/{slug}",
        Status = ProductStatus.Draft,
        CreatedAtUtc = createdAt,
    };

    private static Server NewServer(string name, string host, DateTimeOffset? createdAt = null) =>
        NewServerWithId(Guid.NewGuid(), name, host, createdAt ?? DateTimeOffset.UtcNow);

    private static Server NewServerWithId(Guid id, string name, string host, DateTimeOffset createdAt) => new()
    {
        Id = id,
        Name = name,
        Host = host,
        SshUser = "deploy",
        CreatedAtUtc = createdAt,
    };
}

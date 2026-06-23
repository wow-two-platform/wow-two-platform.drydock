using AwesomeAssertions;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Products.Enums;
using Drydock.Domain.Servers.Entities;
using Drydock.Migrations.Tests.Harness;
using Microsoft.EntityFrameworkCore;

namespace Drydock.Migrations.Tests.Tests;

/// <summary>
/// The unique-index constraints declared on the EF model (<c>ix_products_slug</c>, <c>ix_servers_host</c>) are enforced
/// by the real database — a duplicate insert surfaces as a <see cref="DbUpdateException"/>. Runs on the SDK
/// <see cref="DrydockTestDb"/> (Postgres container or in-memory SQLite); the schema is created by EF from <c>OnModelCreating</c>.
/// </summary>
[Collection(DrydockTestDbCollection.Name)]
public sealed class ConstraintTests(DrydockTestDb db) : IAsyncLifetime
{
    /// <inheritdoc />
    public async Task InitializeAsync() => await db.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DuplicateProductSlug_ViolatesUniqueIndex()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.Products.Add(NewProduct("smart-qr"));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = db.NewContext();
        ctx2.Products.Add(NewProduct("smart-qr")); // same slug, different id.

        var act = async () => await ctx2.SaveChangesAsync();

        // The unique index rejects the duplicate (provider-specific inner exception — assert the EF wrapper only).
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task DuplicateServerHost_ViolatesUniqueIndex()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.Servers.Add(NewServer("first", "10.0.0.1"));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = db.NewContext();
        ctx2.Servers.Add(NewServer("second", "10.0.0.1")); // same host, different id.

        var act = async () => await ctx2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private static Product NewProduct(string slug) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Name = slug,
        Repo = $"wow-two-platform/{slug}",
        Status = ProductStatus.Draft,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    private static Server NewServer(string name, string host) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Host = host,
        SshUser = "deploy",
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}

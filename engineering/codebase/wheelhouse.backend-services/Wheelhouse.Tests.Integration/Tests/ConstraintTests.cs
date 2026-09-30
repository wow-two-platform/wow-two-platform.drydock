using AwesomeAssertions;
using Wheelhouse.Domain.Integrations.Entities;
using Wheelhouse.Domain.Servers.Entities;
using Wheelhouse.Tests.Integration.Harness;
using Microsoft.EntityFrameworkCore;

namespace Wheelhouse.Tests.Integration.Tests;

/// <summary>
/// The unique-index constraints declared on the EF model (<c>ix_integration_keys_hash</c>, <c>ix_servers_host</c>) are enforced
/// by the real database — a duplicate insert surfaces as a <see cref="DbUpdateException"/>. Runs on the SDK
/// <see cref="WheelhouseTestDb"/> (Postgres container or in-memory SQLite); the schema is created by EF from <c>OnModelCreating</c>.
/// </summary>
[Collection(WheelhouseTestDbCollection.Name)]
public sealed class ConstraintTests(WheelhouseTestDb db) : IAsyncLifetime
{
    /// <inheritdoc />
    public async Task InitializeAsync() => await db.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task IntegrationKeyHash_ShouldBeRejected_WhenAnotherKeyHasIt()
    {
        await using (var ctx = db.NewContext())
        {
            ctx.IntegrationKeys.Add(NewKey("first", "hash-1"));
            await ctx.SaveChangesAsync();
        }

        await using var ctx2 = db.NewContext();
        ctx2.IntegrationKeys.Add(NewKey("second", "hash-1")); // same secret hash, different key.

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

    private static IntegrationKeyEntity NewKey(string name, string hash) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Prefix = "wh_abcdefgh",
        Hash = hash,
        Scopes = "catalog:read",
        CreatedBy = "test-admin",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static Server NewServer(string name, string host) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Host = host,
        SshUser = "deploy",
        CreatedAt = DateTimeOffset.UtcNow,
    };
}

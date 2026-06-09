using Drydock.Application.Abstractions;
using Drydock.Domain.Servers.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drydock.Persistence.Stores;

/// <summary>EF Core implementation of <see cref="IServerStore"/>.</summary>
internal sealed class EfServerStore(DrydockDbContext db) : IServerStore
{
    /// <inheritdoc />
    public async Task<Server> AddAsync(Server server, CancellationToken ct = default)
    {
        db.Servers.Add(server);
        await db.SaveChangesAsync(ct);
        return server;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Server>> ListAsync(CancellationToken ct = default) =>
        await db.Servers.AsNoTracking().OrderByDescending(s => s.CreatedAtUtc).ToListAsync(ct);

    /// <inheritdoc />
    public async Task<Server?> FindAsync(Guid id, CancellationToken ct = default) =>
        await db.Servers.FirstOrDefaultAsync(s => s.Id == id, ct);

    /// <inheritdoc />
    public async Task<bool> ExistsByHostAsync(string host, CancellationToken ct = default) =>
        await db.Servers.AnyAsync(s => s.Host == host, ct);
}

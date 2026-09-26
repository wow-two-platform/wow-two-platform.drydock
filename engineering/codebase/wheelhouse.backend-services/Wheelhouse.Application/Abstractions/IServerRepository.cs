using Wheelhouse.Domain.Servers.Entities;

namespace Wheelhouse.Application.Abstractions;

/// <summary>Persistence repository for <see cref="Server"/> aggregates — keeps the application layer off EF Core.</summary>
public interface IServerRepository
{
    /// <summary>Adds a new server and returns it.</summary>
    Task<Server> AddAsync(Server server, CancellationToken ct = default);

    /// <summary>Lists all registered servers, most-recently-created first.</summary>
    Task<IReadOnlyList<Server>> ListAsync(CancellationToken ct = default);

    /// <summary>Finds a server by id, or <see langword="null"/> if none exists.</summary>
    Task<Server?> FindAsync(Guid id, CancellationToken ct = default);

    /// <summary>Removes a server.</summary>
    Task RemoveAsync(Server server, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if a server with the given host already exists.</summary>
    Task<bool> ExistsByHostAsync(string host, CancellationToken ct = default);
}

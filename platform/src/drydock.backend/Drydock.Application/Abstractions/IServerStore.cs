using Drydock.Domain.Servers.Entities;

namespace Drydock.Application.Abstractions;

/// <summary>Persistence gateway for <see cref="Server"/> aggregates — keeps the application layer off EF Core.</summary>
public interface IServerStore
{
    /// <summary>Adds a new server and returns it.</summary>
    Task<Server> AddAsync(Server server, CancellationToken ct = default);

    /// <summary>Lists all registered servers, most-recently-created first.</summary>
    Task<IReadOnlyList<Server>> ListAsync(CancellationToken ct = default);

    /// <summary>Finds a server by id, or <see langword="null"/> if none exists.</summary>
    Task<Server?> FindAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if a server with the given host already exists.</summary>
    Task<bool> ExistsByHostAsync(string host, CancellationToken ct = default);
}

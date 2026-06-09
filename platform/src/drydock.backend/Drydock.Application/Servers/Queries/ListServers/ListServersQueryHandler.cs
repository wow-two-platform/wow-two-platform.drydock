using Drydock.Application.Abstractions;
using Drydock.Application.Servers.Models;
using Drydock.Domain.Results;
using MediatR;

namespace Drydock.Application.Servers.Queries.ListServers;

/// <summary>Handles <see cref="ListServersQuery"/> — projects stored servers into read models.</summary>
internal sealed class ListServersQueryHandler(IServerStore store)
    : IRequestHandler<ListServersQuery, Result<IReadOnlyList<ServerDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ServerDto>>> Handle(ListServersQuery request, CancellationToken ct)
    {
        var servers = await store.ListAsync(ct);

        IReadOnlyList<ServerDto> dtos = servers
            .Select(s => new ServerDto(s.Id, s.Name, s.Host, s.SshUser, s.Region, s.Status, s.CreatedAtUtc))
            .ToList();

        return Result<IReadOnlyList<ServerDto>>.Ok(dtos);
    }
}

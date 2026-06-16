using Drydock.Application.Abstractions;
using Drydock.Application.Servers.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Queries.ServerGetAll;

/// <summary>Handles <see cref="ServerGetAllQuery"/>.</summary>
public sealed class ServerGetAllQueryHandler(IServerStore store)
    : IQueryHandler<ServerGetAllQuery, AppResult<ServerGetAllResult.Success, ServerGetAllResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ServerGetAllResult.Success, ServerGetAllResult.Failure>> HandleAsync(
        ServerGetAllQuery request, CancellationToken cancellationToken)
    {
        var servers = await store.ListAsync(cancellationToken);

        IReadOnlyList<ServerDto> dtos = servers
            .Select(s => new ServerDto(s.Id, s.Name, s.Host, s.SshUser, s.Region, s.Status, s.CreatedAtUtc))
            .ToList();

        return new AppResult<ServerGetAllResult.Success, ServerGetAllResult.Failure>.Success(
            new ServerGetAllResult.Success(dtos));
    }
}

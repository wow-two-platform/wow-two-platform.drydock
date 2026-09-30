using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Servers.Models;
using Wheelhouse.Domain.Servers.Entities;
using Wheelhouse.Domain.Servers.Enums;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Servers.Commands.ServerRegister;

/// <summary>Handles <see cref="ServerRegisterCommand"/>.</summary>
public sealed class ServerRegisterCommandHandler(IServerRepository store)
    : ICommandHandler<ServerRegisterCommand, AppResult<ServerRegisterResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ServerRegisterResult>> HandleAsync(
        ServerRegisterCommand request, CancellationToken cancellationToken)
    {
        if (await store.ExistsByHostAsync(request.Host.Trim(), cancellationToken))
            return AppResult<ServerRegisterResult>.Fail(AppErrorFactory.Conflict($"A server with host '{request.Host}' already exists."));

        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Host = request.Host.Trim(),
            SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser.Trim(),
            // The validator has already rejected any explicitly out-of-range port; only an omitted 0 reaches here → default 22.
            SshPort = ServerValidation.IsValidPort(request.SshPort) ? request.SshPort : 22,
            Region = request.Region,
            Status = ServerStatus.Unknown
            // CreatedAt is stamped by the SDK audit interceptor on SaveChanges — not hand-set.
        };

        await store.AddAsync(server, cancellationToken);

        return AppResult<ServerRegisterResult>.Ok(new ServerRegisterResult(new ServerDto(
            server.Id, server.Name, server.Host, server.SshUser, server.Region, server.Status, server.CreatedAt)));
    }
}

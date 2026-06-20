using Drydock.Application.Abstractions;
using Drydock.Application.Servers.Models;
using Drydock.Domain.Results;
using Drydock.Domain.Servers.Entities;
using Drydock.Domain.Servers.Enums;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Commands.ServerRegister;

/// <summary>Handles <see cref="ServerRegisterCommand"/>.</summary>
public sealed class ServerRegisterCommandHandler(IServerStore store, TimeProvider timeProvider)
    : ICommandHandler<ServerRegisterCommand, AppResult<ServerRegisterResult.Success, ServerRegisterResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ServerRegisterResult.Success, ServerRegisterResult.Failure>> HandleAsync(
        ServerRegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Fail(FailureCategory.Validation, "Name is required.");
        if (string.IsNullOrWhiteSpace(request.Host))
            return Fail(FailureCategory.Validation, "Host is required.");
        if (await store.ExistsByHostAsync(request.Host.Trim(), cancellationToken))
            return Fail(FailureCategory.Conflict, $"A server with host '{request.Host}' already exists.");

        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Host = request.Host.Trim(),
            SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser.Trim(),
            SshPort = request.SshPort is > 0 and <= 65535 ? request.SshPort : 22,
            Region = request.Region,
            Status = ServerStatus.Unknown,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        await store.AddAsync(server, cancellationToken);

        return new AppResult<ServerRegisterResult.Success, ServerRegisterResult.Failure>.Success(
            new ServerRegisterResult.Success(new ServerDto(
                server.Id, server.Name, server.Host, server.SshUser, server.Region, server.Status, server.CreatedAtUtc)));
    }

    private static AppResult<ServerRegisterResult.Success, ServerRegisterResult.Failure> Fail(
        FailureCategory category, string message) =>
        new AppResult<ServerRegisterResult.Success, ServerRegisterResult.Failure>.Failure(
            new ServerRegisterResult.Failure(message, category));
}

using Drydock.Application.Abstractions;
using Drydock.Application.Servers.Models;
using Drydock.Domain.Results;
using Drydock.Domain.Servers.Entities;
using Drydock.Domain.Servers.Enums;
using MediatR;

namespace Drydock.Application.Servers.Commands.RegisterServer;

/// <summary>Handles <see cref="RegisterServerCommand"/> — validates and persists a new server.</summary>
internal sealed class RegisterServerCommandHandler(IServerStore store, IClock clock)
    : IRequestHandler<RegisterServerCommand, Result<ServerDto>>
{
    /// <inheritdoc />
    public async Task<Result<ServerDto>> Handle(RegisterServerCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<ServerDto>.Fail(ResultError.Validation, "Name is required.");
        if (string.IsNullOrWhiteSpace(request.Host))
            return Result<ServerDto>.Fail(ResultError.Validation, "Host is required.");
        if (await store.ExistsByHostAsync(request.Host.Trim(), ct))
            return Result<ServerDto>.Fail(ResultError.Conflict, $"A server with host '{request.Host}' already exists.");

        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Host = request.Host.Trim(),
            SshUser = string.IsNullOrWhiteSpace(request.SshUser) ? "root" : request.SshUser.Trim(),
            SshPort = request.SshPort is > 0 and <= 65535 ? request.SshPort : 22,
            Region = request.Region,
            Status = ServerStatus.Unknown,
            CreatedAtUtc = clock.UtcNow
        };

        await store.AddAsync(server, ct);

        return Result<ServerDto>.Ok(new ServerDto(
            server.Id, server.Name, server.Host, server.SshUser, server.Region, server.Status, server.CreatedAtUtc));
    }
}

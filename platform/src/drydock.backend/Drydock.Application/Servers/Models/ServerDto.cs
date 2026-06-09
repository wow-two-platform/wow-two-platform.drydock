using Drydock.Domain.Servers.Enums;

namespace Drydock.Application.Servers.Models;

/// <summary>Read model for a registered server.</summary>
/// <param name="Id">Server id.</param>
/// <param name="Name">Friendly label.</param>
/// <param name="Host">IP or hostname.</param>
/// <param name="SshUser">Deploy user.</param>
/// <param name="Region">Hetzner region, when known.</param>
/// <param name="Status">Connectivity state.</param>
/// <param name="CreatedAtUtc">When the server was registered.</param>
public sealed record ServerDto(
    Guid Id,
    string Name,
    string Host,
    string SshUser,
    string? Region,
    ServerStatus Status,
    DateTimeOffset CreatedAtUtc);

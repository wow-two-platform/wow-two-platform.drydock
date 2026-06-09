using Drydock.Domain.Common;
using Drydock.Domain.Servers.Enums;

namespace Drydock.Domain.Servers.Entities;

/// <summary>A Hetzner VPS that Drydock deploys products onto over SSH.</summary>
public sealed class Server : IKeyedEntity<Guid>
{
    /// <summary>Gets the server's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the friendly label (e.g. <c>hel1-prod</c>).</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the IP address or hostname.</summary>
    public required string Host { get; set; }

    /// <summary>Gets or sets the SSH port.</summary>
    public int SshPort { get; set; } = 22;

    /// <summary>Gets or sets the deploy user (a least-privilege user, or <c>root</c> on a fresh box).</summary>
    public required string SshUser { get; set; }

    /// <summary>Gets or sets the id of the <c>SecretEntry</c> holding this server's SSH private key.</summary>
    public Guid? SshKeySecretId { get; set; }

    /// <summary>Gets or sets the Hetzner Cloud server id, when Drydock provisioned the box.</summary>
    public string? HetznerServerId { get; set; }

    /// <summary>Gets or sets the Hetzner region/location (e.g. <c>hel1</c>).</summary>
    public string? Region { get; set; }

    /// <summary>Gets or sets the current connectivity state.</summary>
    public ServerStatus Status { get; set; } = ServerStatus.Unknown;

    /// <summary>Gets the UTC instant the server was registered.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets or sets the UTC instant of the last reachability check.</summary>
    public DateTimeOffset? LastCheckedAtUtc { get; set; }
}

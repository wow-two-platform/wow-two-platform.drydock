namespace Drydock.Api.Requests;

/// <summary>Request body for registering a server.</summary>
/// <param name="Name">Friendly label.</param>
/// <param name="Host">IP or hostname.</param>
/// <param name="SshUser">Deploy user (defaults to <c>root</c> when blank).</param>
/// <param name="SshPort">SSH port (defaults to 22 when out of range).</param>
/// <param name="Region">Optional Hetzner region.</param>
public sealed record RegisterServerRequest(
    string Name,
    string Host,
    string SshUser,
    int SshPort,
    string? Region);

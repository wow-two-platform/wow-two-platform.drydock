using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Commands.ServerRegister;

/// <summary>Represents a command to register a Hetzner VPS as a deploy target.</summary>
/// <param name="Name">Friendly label.</param>
/// <param name="Host">IP or hostname.</param>
/// <param name="SshUser">Deploy user (defaults to <c>root</c> when blank).</param>
/// <param name="SshPort">SSH port (defaults to 22 when out of range).</param>
/// <param name="Region">Optional Hetzner region.</param>
public sealed record ServerRegisterCommand(
    string Name,
    string Host,
    string SshUser,
    int SshPort,
    string? Region) : ICommand<AppResult<ServerRegisterResult.Success, ServerRegisterResult.Failure>>;

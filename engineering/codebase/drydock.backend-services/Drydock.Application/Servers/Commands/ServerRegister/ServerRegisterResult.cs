using Drydock.Application.Servers.Models;

namespace Drydock.Application.Servers.Commands.ServerRegister;

/// <summary>Success payload of registering a server — carried by the operation's <c>AppResult&lt;ServerRegisterResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Server">The registered server.</param>
public sealed record ServerRegisterResult(ServerDto Server);

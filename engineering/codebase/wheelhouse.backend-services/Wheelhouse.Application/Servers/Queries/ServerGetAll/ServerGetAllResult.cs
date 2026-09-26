using Wheelhouse.Application.Servers.Models;

namespace Wheelhouse.Application.Servers.Queries.ServerGetAll;

/// <summary>Success payload of listing all servers — carried by the operation's <c>AppResult&lt;ServerGetAllResult&gt;</c>; failures surface as an <c>AppError</c>.</summary>
/// <param name="Servers">The registered servers.</param>
public sealed record ServerGetAllResult(IReadOnlyList<ServerDto> Servers);

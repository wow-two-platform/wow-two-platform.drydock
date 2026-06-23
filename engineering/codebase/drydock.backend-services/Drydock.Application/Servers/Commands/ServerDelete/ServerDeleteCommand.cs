using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Commands.ServerDelete;

/// <summary>Represents a command to delete a server by id.</summary>
/// <param name="Id">Server id.</param>
public sealed record ServerDeleteCommand(Guid Id)
    : ICommand<AppResult<ServerDeleteResult.Success, ServerDeleteResult.Failure>>;

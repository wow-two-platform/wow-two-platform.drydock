using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Queries.ServerGetAll;

/// <summary>Represents a query to get all servers.</summary>
public sealed record ServerGetAllQuery
    : IQuery<AppResult<ServerGetAllResult.Success, ServerGetAllResult.Failure>>;

using Drydock.Application.Servers.Models;
using Drydock.Domain.Results;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Queries.ServerGetAll;

/// <summary>Outcome of listing all servers.</summary>
public abstract record ServerGetAllResult
{
    private ServerGetAllResult() { }

    /// <summary>The servers were listed.</summary>
    public sealed record Success(IReadOnlyList<ServerDto> Servers) : ServerGetAllResult, ISuccessResult;

    /// <summary>The servers could not be listed — <see cref="IDrydockFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ServerGetAllResult, IDrydockFailure;
}

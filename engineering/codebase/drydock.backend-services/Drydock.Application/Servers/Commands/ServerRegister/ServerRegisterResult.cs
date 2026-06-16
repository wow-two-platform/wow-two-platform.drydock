using Drydock.Application.Servers.Models;
using Drydock.Domain.Results;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Commands.ServerRegister;

/// <summary>Outcome of registering a server.</summary>
public abstract record ServerRegisterResult
{
    private ServerRegisterResult() { }

    /// <summary>The server was registered.</summary>
    public sealed record Success(ServerDto Server) : ServerRegisterResult, ISuccessResult;

    /// <summary>The server could not be registered — <see cref="IDrydockFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ServerRegisterResult, IDrydockFailure;
}

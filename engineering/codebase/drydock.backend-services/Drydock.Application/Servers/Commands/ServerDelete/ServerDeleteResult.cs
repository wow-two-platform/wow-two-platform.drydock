using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Commands.ServerDelete;

/// <summary>Outcome of deleting a server.</summary>
public abstract record ServerDeleteResult
{
    private ServerDeleteResult() { }

    /// <summary>The server was deleted — no payload, the controller maps it to <c>NoContent</c>.</summary>
    public sealed record Success : ServerDeleteResult, ISuccessResult;

    /// <summary>The server could not be deleted — <see cref="ICategorizedFailure.Category"/> maps the status.</summary>
    public sealed record Failure(string ErrorMessage, FailureCategory Category) : ServerDeleteResult, ICategorizedFailure;
}

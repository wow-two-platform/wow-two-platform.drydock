using Drydock.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Servers.Commands.ServerDelete;

/// <summary>Handles <see cref="ServerDeleteCommand"/>.</summary>
public sealed class ServerDeleteCommandHandler(IServerStore store)
    : ICommandHandler<ServerDeleteCommand, AppResult<ServerDeleteResult.Success, ServerDeleteResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ServerDeleteResult.Success, ServerDeleteResult.Failure>> HandleAsync(
        ServerDeleteCommand request, CancellationToken cancellationToken)
    {
        var server = await store.FindAsync(request.Id, cancellationToken);
        if (server is null)
            return new AppResult<ServerDeleteResult.Success, ServerDeleteResult.Failure>.Failure(
                new ServerDeleteResult.Failure($"Server '{request.Id}' was not found.", FailureCategory.NotFound));

        await store.RemoveAsync(server, cancellationToken);

        return new AppResult<ServerDeleteResult.Success, ServerDeleteResult.Failure>.Success(
            new ServerDeleteResult.Success());
    }
}

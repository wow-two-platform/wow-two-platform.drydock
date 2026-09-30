using Wheelhouse.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Servers.Commands.ServerDelete;

/// <summary>Handles <see cref="ServerDeleteCommand"/>.</summary>
public sealed class ServerDeleteCommandHandler(IServerRepository store)
    : ICommandHandler<ServerDeleteCommand, AppResult<ServerDeleteResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ServerDeleteResult>> HandleAsync(
        ServerDeleteCommand request, CancellationToken cancellationToken)
    {
        var server = await store.FindAsync(request.Id, cancellationToken);
        if (server is null)
            return AppResult<ServerDeleteResult>.Fail(AppErrorFactory.NotFound($"Server '{request.Id}' was not found."));

        await store.RemoveAsync(server, cancellationToken);

        return AppResult<ServerDeleteResult>.Ok(new ServerDeleteResult());
    }
}

using System.Text.Json;
using Drydock.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Deployments;

/// <summary>Handles deployment inventory and outcome queries.</summary>
public sealed class DeploymentReadQueryHandler(IDeploymentGateway gateway)
    : IQueryHandler<DeploymentReadQuery, AppResult<JsonElement>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<JsonElement>> HandleAsync(DeploymentReadQuery request, CancellationToken cancellationToken) =>
        await gateway.ReadAsync(request.Resource, request.Id, cancellationToken);
}

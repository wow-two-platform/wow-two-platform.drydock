using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Deployments;

/// <summary>Reads a deployment resource from the operator runner.</summary>
public sealed record DeploymentReadQuery(string Resource, string? Id = null) : IQuery<AppResult<JsonElement>>;

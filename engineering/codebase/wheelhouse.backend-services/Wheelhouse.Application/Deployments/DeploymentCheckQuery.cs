using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Deployments;

/// <summary>Checks one configured target's readiness, optionally against one release.</summary>
public sealed record DeploymentCheckQuery(string Target, string? Release) : IQuery<AppResult<JsonElement>>;

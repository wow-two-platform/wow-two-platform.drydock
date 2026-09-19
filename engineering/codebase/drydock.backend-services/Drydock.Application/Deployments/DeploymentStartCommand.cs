using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Deployments;

/// <summary>Submits one reviewed release to one configured target.</summary>
public sealed record DeploymentStartCommand(string Target, string Release, string Actor) : ICommand<AppResult<JsonElement>>;

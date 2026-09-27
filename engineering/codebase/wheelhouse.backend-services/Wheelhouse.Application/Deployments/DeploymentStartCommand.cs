using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Deployments;

/// <summary>Submits one reviewed release to one configured target; <paramref name="Confirm"/> is the typed target ID.</summary>
public sealed record DeploymentStartCommand(string Target, string Release, string Actor, string? Confirm = null)
    : ICommand<AppResult<JsonElement>>;

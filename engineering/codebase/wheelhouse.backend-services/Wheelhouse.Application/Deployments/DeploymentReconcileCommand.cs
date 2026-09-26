using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Deployments;

/// <summary>Acknowledges an interrupted or failed rollout after the operator inspected its target.</summary>
public sealed record DeploymentReconcileCommand(string Target, string Job, string Actor) : ICommand<AppResult<JsonElement>>;

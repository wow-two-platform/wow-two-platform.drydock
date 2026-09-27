using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Deployments;

/// <summary>Starts a product's build workflow for a commit that has no build yet.</summary>
public sealed record DeploymentBuildCommand(string Product, string Commit, string Actor) : ICommand<AppResult<JsonElement>>;

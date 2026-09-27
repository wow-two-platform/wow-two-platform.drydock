using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Deployments;

/// <summary>Submits one reviewed release to one configured target; <paramref name="Confirm"/> is the typed target ID,
/// and <paramref name="SkipTestPass"/> lets prod take a release that has not succeeded on test.</summary>
public sealed record DeploymentStartCommand(
    string Target, string Release, string Actor, string? Confirm = null, bool SkipTestPass = false)
    : ICommand<AppResult<JsonElement>>;

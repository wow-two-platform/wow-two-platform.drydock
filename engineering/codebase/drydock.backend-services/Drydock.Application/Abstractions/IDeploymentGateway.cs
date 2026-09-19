using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Abstractions;

/// <summary>Exposes the operator runner through the control plane.</summary>
public interface IDeploymentGateway
{
    /// <summary>Reads the configured targets, releases or one deployment status.</summary>
    Task<AppResult<JsonElement>> ReadAsync(string resource, string? id, CancellationToken ct);
    /// <summary>Submits a trusted release to a configured target.</summary>
    Task<AppResult<JsonElement>> StartAsync(string target, string release, string actor, CancellationToken ct);
}

using System.Text.Json;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Abstractions;

/// <summary>Exposes the operator runner through the control plane.</summary>
public interface IDeploymentGateway
{
    /// <summary>Reads the fleet, targets, releases, deployment history and statistics, one outcome, one target's state, or host and container vitals.</summary>
    Task<AppResult<JsonElement>> ReadAsync(string resource, string? id, CancellationToken ct);
    /// <summary>Checks a target's readiness without changing it, optionally against one release.</summary>
    Task<AppResult<JsonElement>> CheckAsync(string target, string? release, CancellationToken ct);
    /// <summary>Submits a trusted release to a configured target.</summary>
    Task<AppResult<JsonElement>> StartAsync(string target, string release, string actor, CancellationToken ct);
    /// <summary>Records the operator's reconciliation of an interrupted or failed rollout on its target.</summary>
    Task<AppResult<JsonElement>> ReconcileAsync(string target, string job, string actor, CancellationToken ct);
}

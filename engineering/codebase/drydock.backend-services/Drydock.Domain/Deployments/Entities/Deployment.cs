using Drydock.Domain.Common;
using Drydock.Domain.Deployments.Enums;

namespace Drydock.Domain.Deployments.Entities;

/// <summary>One attempt to ship a product's images to a server, with streamed logs and outcome.</summary>
public sealed class Deployment : IKeyedEntity<Guid>
{
    /// <summary>Gets the deployment's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the product being deployed.</summary>
    public required Guid ProductId { get; init; }

    /// <summary>Gets the target server.</summary>
    public required Guid ServerId { get; init; }

    /// <summary>Gets the environment name (e.g. <c>prod</c>, <c>staging</c>).</summary>
    public string Environment { get; init; } = "prod";

    /// <summary>Gets or sets the frontend image tag (git SHA) deployed.</summary>
    public string? ImageWebTag { get; set; }

    /// <summary>Gets or sets the backend image tag (git SHA) deployed.</summary>
    public string? ImageApiTag { get; set; }

    /// <summary>Gets or sets the job status.</summary>
    public DeploymentStatus Status { get; set; } = DeploymentStatus.Queued;

    /// <summary>Gets or sets the captured stdout (also streamed live over SignalR).</summary>
    public string? Log { get; set; }

    /// <summary>Gets or sets who/what triggered the deployment.</summary>
    public string? TriggeredBy { get; set; }

    /// <summary>Gets the UTC instant the deployment was queued.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets or sets the UTC instant the deployment finished.</summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

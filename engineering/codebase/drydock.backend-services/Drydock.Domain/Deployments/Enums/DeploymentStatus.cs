namespace Drydock.Domain.Deployments.Enums;

/// <summary>Progress of a deployment job.</summary>
public enum DeploymentStatus
{
    /// <summary>Accepted, waiting for a worker.</summary>
    Queued = 0,

    /// <summary>Executing on the target server.</summary>
    Running,

    /// <summary>Containers are up and healthy.</summary>
    Succeeded,

    /// <summary>The attempt failed.</summary>
    Failed,

    /// <summary>Reverted to the previous image tag.</summary>
    RolledBack
}

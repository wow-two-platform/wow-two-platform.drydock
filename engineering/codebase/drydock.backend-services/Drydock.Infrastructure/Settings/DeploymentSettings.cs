namespace Drydock.Infrastructure.Settings;

/// <summary>Holds operator-owned runner paths; HTTP clients cannot choose them.</summary>
public sealed record DeploymentSettings
{
    /// <summary>Gets the executable used for the Python runner.</summary>
    public string Python { get; init; } = "python3";
    /// <summary>Gets the installed SSH adapter path.</summary>
    public string TransportPath { get; init; } = "/app/runner/transport.py";
    /// <summary>Gets the protected inventory, release and audit root.</summary>
    public string Root { get; init; } = "/data/deployments";
}

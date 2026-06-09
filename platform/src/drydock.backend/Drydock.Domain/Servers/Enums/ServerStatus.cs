namespace Drydock.Domain.Servers.Enums;

/// <summary>Connectivity / health state of a registered server.</summary>
public enum ServerStatus
{
    /// <summary>Not yet checked.</summary>
    Unknown = 0,

    /// <summary>SSH connected and Docker responded.</summary>
    Reachable,

    /// <summary>Last connection attempt failed.</summary>
    Unreachable,

    /// <summary>A Hetzner Cloud create/bootstrap is in progress.</summary>
    Provisioning
}

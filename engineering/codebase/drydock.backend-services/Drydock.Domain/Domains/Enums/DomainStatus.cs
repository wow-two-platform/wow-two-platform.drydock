namespace Drydock.Domain.Domains.Enums;

/// <summary>State of a managed domain.</summary>
public enum DomainStatus
{
    /// <summary>Searched and available, not yet purchased.</summary>
    Available = 0,

    /// <summary>Purchased through the registrar.</summary>
    Owned,

    /// <summary>Wired to a product environment (DNS + Traefik route).</summary>
    Assigned,

    /// <summary>Within the renewal warning window.</summary>
    Expiring
}

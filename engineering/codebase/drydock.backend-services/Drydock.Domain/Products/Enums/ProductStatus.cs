namespace Drydock.Domain.Products.Enums;

/// <summary>Lifecycle state of a portfolio product.</summary>
public enum ProductStatus
{
    /// <summary>Registered but never deployed.</summary>
    Draft = 0,

    /// <summary>Live and maintained.</summary>
    Active,

    /// <summary>Temporarily stopped.</summary>
    Paused,

    /// <summary>Retired through the kill-gate (containers stopped, repo archived).</summary>
    Killed
}

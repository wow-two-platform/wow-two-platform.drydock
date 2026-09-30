namespace Wheelhouse.Domain.Products.Enums;

/// <summary>Refers to where a product stands in the portfolio.</summary>
public enum ProductLifecycle
{
    /// <summary>Considered, with nothing built yet.</summary>
    Idea,

    /// <summary>Being built toward its first release.</summary>
    Building,

    /// <summary>Released and serving its users.</summary>
    Live,

    /// <summary>Released, with work on it stopped for now.</summary>
    Paused,

    /// <summary>Stopped for good and kept for its history.</summary>
    Killed,
}

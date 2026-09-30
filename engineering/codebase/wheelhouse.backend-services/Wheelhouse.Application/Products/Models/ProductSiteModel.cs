namespace Wheelhouse.Application.Products.Models;

/// <summary>Represents a site an environment publishes.</summary>
public sealed record ProductSiteModel
{
    /// <summary>Gets the site's name, such as <c>app</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the site's address.</summary>
    public required string Url { get; init; }

    /// <summary>Gets <c>public</c>, or <c>private</c> for a site reached over the private network only.</summary>
    public required string Exposure { get; init; }
}

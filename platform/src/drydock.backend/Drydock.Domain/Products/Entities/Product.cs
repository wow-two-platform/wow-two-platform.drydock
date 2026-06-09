using Drydock.Domain.Common;
using Drydock.Domain.Products.Enums;

namespace Drydock.Domain.Products.Entities;

/// <summary>A deployable product in the portfolio — a frontend + backend pair shipped as containers.</summary>
public sealed class Product : IKeyedEntity<Guid>
{
    /// <summary>Gets the product's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the URL-safe slug (unique).</summary>
    public required string Slug { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the GitHub repository for the frontend.</summary>
    public string? RepoWeb { get; set; }

    /// <summary>Gets or sets the GitHub repository for the backend.</summary>
    public string? RepoApi { get; set; }

    /// <summary>Gets or sets the GHCR image (without tag) for the frontend.</summary>
    public string? ImageWeb { get; set; }

    /// <summary>Gets or sets the GHCR image (without tag) for the backend.</summary>
    public string? ImageApi { get; set; }

    /// <summary>Gets or sets the lifecycle state.</summary>
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    /// <summary>Gets the UTC instant the product was registered.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}

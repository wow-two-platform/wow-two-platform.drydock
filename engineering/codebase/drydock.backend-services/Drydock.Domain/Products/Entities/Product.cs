using Drydock.Domain.Common;
using Drydock.Domain.Products.Enums;

namespace Drydock.Domain.Products.Entities;

/// <summary>A deployable product in the portfolio — one GitHub repo shipped as a single container (single-host).</summary>
public sealed class Product : IKeyedEntity<Guid>
{
    /// <summary>Gets the product's unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or sets the URL-safe slug (unique).</summary>
    public required string Slug { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the GitHub repository that defines the product, as <c>{owner}/{repo}</c> (e.g. <c>wow-two-platform/wow-two-platform.secrets-vault</c>). The deployable image is derived from it later.</summary>
    public required string Repo { get; set; }

    /// <summary>Gets or sets the lifecycle state.</summary>
    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    /// <summary>Gets the UTC instant the product was registered.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}

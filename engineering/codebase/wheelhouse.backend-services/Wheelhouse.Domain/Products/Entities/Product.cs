using WoW.Two.Sdk.Backend.Beta.Data.Abstractions;
using Wheelhouse.Domain.Products.Enums;

namespace Wheelhouse.Domain.Products.Entities;

/// <summary>A deployable product in the portfolio — one GitHub repo shipped as a single container (single-host).</summary>
public sealed class Product : IKeyedEntity<Guid>, IHasTableName, IAuditable
{
    /// <summary>Gets the storage table name — the single source of truth shared by EF mapping and hand-written SQL.</summary>
    public static string TableName => "products";

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

    /// <summary>Gets or sets the UTC instant the product was registered. Stamped by the SDK audit interceptor on insert
    /// (column <c>created_at_utc</c>); never hand-set.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC instant of the last change. Stamped by the SDK audit interceptor on insert and update
    /// (column <c>updated_at_utc</c>); never hand-set.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

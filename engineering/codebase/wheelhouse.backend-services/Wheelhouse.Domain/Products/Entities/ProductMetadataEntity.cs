using Wheelhouse.Domain.Products.Enums;
using WoW.Two.Sdk.Backend.Beta.Data.Abstractions;

namespace Wheelhouse.Domain.Products.Entities;

/// <summary>Represents what the operator records about a catalog product — the facts that change without a code
/// review. The product's identity lives in the code-owned catalog.</summary>
public sealed record ProductMetadataEntity : IKeyedEntity<string>, IHasTableName, IAuditable
{
    /// <summary>Gets the storage table name, shared by the EF mapping and the SQL migrations.</summary>
    public static string TableName => "product_metadata";

    /// <summary>Gets or sets the slug of the catalog product the row describes.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets where the product stands in the portfolio.</summary>
    public ProductLifecycle Lifecycle { get; set; } = ProductLifecycle.Building;

    /// <summary>Gets or sets when the row was created; stamped by the SDK audit interceptor.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the row last changed; stamped by the SDK audit interceptor.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

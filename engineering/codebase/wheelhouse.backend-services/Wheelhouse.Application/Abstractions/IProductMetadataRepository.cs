using Wheelhouse.Domain.Products.Entities;
using Wheelhouse.Domain.Products.Enums;

namespace Wheelhouse.Application.Abstractions;

/// <summary>Defines the store of what the operator records about catalog products, keyed by slug.</summary>
public interface IProductMetadataRepository
{
    /// <summary>Lists every recorded row.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The rows, in no particular order.</returns>
    Task<IReadOnlyList<ProductMetadataEntity>> ListAsync(CancellationToken ct = default);

    /// <summary>Finds the row for a slug.</summary>
    /// <param name="slug">The product's slug.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The row, or <c>null</c> when the operator never recorded one.</returns>
    Task<ProductMetadataEntity?> FindAsync(string slug, CancellationToken ct = default);

    /// <summary>Records a product's lifecycle, creating its row on first use.</summary>
    /// <param name="slug">The product's slug.</param>
    /// <param name="lifecycle">Where the product now stands.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The saved row.</returns>
    Task<ProductMetadataEntity> SetLifecycleAsync(string slug, ProductLifecycle lifecycle, CancellationToken ct = default);
}

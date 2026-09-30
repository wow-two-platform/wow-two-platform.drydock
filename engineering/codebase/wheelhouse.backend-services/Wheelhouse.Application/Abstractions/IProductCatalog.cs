using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Abstractions;

/// <summary>Defines the reader of the code-owned product catalog: each product's identity and the environments its
/// fleet targets bind.</summary>
public interface IProductCatalog
{
    /// <summary>Lists the catalog's products in the order the code declares them.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The products, or a failure when the catalog cannot be read.</returns>
    Task<AppResult<IReadOnlyList<ProductDefinitionModel>>> ListAsync(CancellationToken ct);

    /// <summary>Finds the product the catalog names <paramref name="slug"/>.</summary>
    /// <param name="slug">The product's slug.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The product, not found when the catalog does not define it, or a failure when it cannot be read.</returns>
    Task<AppResult<ProductDefinitionModel>> FindAsync(string slug, CancellationToken ct);
}

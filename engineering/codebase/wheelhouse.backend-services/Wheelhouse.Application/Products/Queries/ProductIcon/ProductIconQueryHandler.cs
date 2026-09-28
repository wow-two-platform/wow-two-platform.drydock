using Wheelhouse.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Queries.ProductIcon;

/// <summary>Handles <see cref="ProductIconQuery"/>: not found when the product is unknown or its repository has no icon.</summary>
public sealed class ProductIconQueryHandler(IProductRepository store, IProductIconSource icons)
    : IQueryHandler<ProductIconQuery, AppResult<ProductIconImage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductIconImage>> HandleAsync(ProductIconQuery request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return AppResult<ProductIconImage>.Fail(AppErrors.NotFound($"Product '{request.Id}' was not found."));

        var icon = await icons.FindAsync(product.Repo, cancellationToken);
        return icon is null
            ? AppResult<ProductIconImage>.Fail(AppErrors.NotFound("The product's repository carries no icon."))
            : AppResult<ProductIconImage>.Ok(icon);
    }
}

using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Models;
using Wheelhouse.Application.Products.Queries;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.QueryHandlers;

/// <summary>Handles <see cref="ProductIconQuery"/>; not found when the catalog lacks the product or its repository
/// carries no icon.</summary>
public sealed class ProductIconQueryHandler(IProductCatalog catalog, IProductIconSource icons)
    : IQueryHandler<ProductIconQuery, AppResult<ProductIconImage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductIconImage>> HandleAsync(ProductIconQuery request, CancellationToken cancellationToken)
    {
        var found = await catalog.FindAsync(request.Slug, cancellationToken);
        if (found is not AppResult<ProductDefinitionModel>.Success { Data: var definition })
            return AppResult<ProductIconImage>.Fail(((AppResult<ProductDefinitionModel>.Failure)found).Error);

        var icon = await icons.FindAsync(definition.Repository, cancellationToken);
        return icon is null
            ? AppResult<ProductIconImage>.Fail(AppErrorFactory.NotFound("The product's repository carries no icon."))
            : AppResult<ProductIconImage>.Ok(icon);
    }
}

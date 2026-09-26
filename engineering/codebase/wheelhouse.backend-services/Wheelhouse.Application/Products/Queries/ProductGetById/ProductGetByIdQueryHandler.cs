using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Queries.ProductGetById;

/// <summary>Handles <see cref="ProductGetByIdQuery"/>.</summary>
public sealed class ProductGetByIdQueryHandler(IProductRepository store)
    : IQueryHandler<ProductGetByIdQuery, AppResult<ProductGetByIdResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductGetByIdResult>> HandleAsync(
        ProductGetByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return AppResult<ProductGetByIdResult>.Fail(AppErrors.NotFound($"Product '{request.Id}' was not found."));

        return AppResult<ProductGetByIdResult>.Ok(new ProductGetByIdResult(new ProductDto(
            product.Id, product.Slug, product.Name, product.Repo, product.Status, product.CreatedAt)));
    }
}

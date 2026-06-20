using Drydock.Application.Abstractions;
using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductGetById;

/// <summary>Handles <see cref="ProductGetByIdQuery"/>.</summary>
public sealed class ProductGetByIdQueryHandler(IProductStore store)
    : IQueryHandler<ProductGetByIdQuery, AppResult<ProductGetByIdResult.Success, ProductGetByIdResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductGetByIdResult.Success, ProductGetByIdResult.Failure>> HandleAsync(
        ProductGetByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return new AppResult<ProductGetByIdResult.Success, ProductGetByIdResult.Failure>.Failure(
                new ProductGetByIdResult.Failure($"Product '{request.Id}' was not found.", FailureCategory.NotFound));

        return new AppResult<ProductGetByIdResult.Success, ProductGetByIdResult.Failure>.Success(
            new ProductGetByIdResult.Success(new ProductDto(
                product.Id, product.Slug, product.Name, product.Repo, product.Status, product.CreatedAtUtc)));
    }
}

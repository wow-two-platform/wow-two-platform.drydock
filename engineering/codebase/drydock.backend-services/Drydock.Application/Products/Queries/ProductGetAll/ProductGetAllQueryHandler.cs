using Drydock.Application.Abstractions;
using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductGetAll;

/// <summary>Handles <see cref="ProductGetAllQuery"/>.</summary>
public sealed class ProductGetAllQueryHandler(IProductStore store)
    : IQueryHandler<ProductGetAllQuery, AppResult<ProductGetAllResult.Success, ProductGetAllResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductGetAllResult.Success, ProductGetAllResult.Failure>> HandleAsync(
        ProductGetAllQuery request, CancellationToken cancellationToken)
    {
        var products = await store.ListAsync(cancellationToken);

        IReadOnlyList<ProductDto> dtos = products
            .Select(p => new ProductDto(p.Id, p.Slug, p.Name, p.Repo, p.Status, p.CreatedAtUtc))
            .ToList();

        return new AppResult<ProductGetAllResult.Success, ProductGetAllResult.Failure>.Success(
            new ProductGetAllResult.Success(dtos));
    }
}

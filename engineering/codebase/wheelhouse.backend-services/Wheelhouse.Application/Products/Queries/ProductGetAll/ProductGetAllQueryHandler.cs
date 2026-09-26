using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Queries.ProductGetAll;

/// <summary>Handles <see cref="ProductGetAllQuery"/>.</summary>
public sealed class ProductGetAllQueryHandler(IProductRepository store)
    : IQueryHandler<ProductGetAllQuery, AppResult<ProductGetAllResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductGetAllResult>> HandleAsync(
        ProductGetAllQuery request, CancellationToken cancellationToken)
    {
        var products = await store.ListAsync(cancellationToken);

        IReadOnlyList<ProductDto> dtos = products
            .Select(p => new ProductDto(p.Id, p.Slug, p.Name, p.Repo, p.Status, p.CreatedAt))
            .ToList();

        return AppResult<ProductGetAllResult>.Ok(new ProductGetAllResult(dtos));
    }
}

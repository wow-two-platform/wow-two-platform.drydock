using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Mappers;
using Wheelhouse.Application.Products.Models;
using Wheelhouse.Application.Products.Queries;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.QueryHandlers;

/// <summary>Handles <see cref="ProductGetAllQuery"/>.</summary>
public sealed class ProductGetAllQueryHandler(IProductCatalog catalog, IProductMetadataRepository metadata)
    : IQueryHandler<ProductGetAllQuery, AppResult<IReadOnlyList<ProductDto>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<ProductDto>>> HandleAsync(
        ProductGetAllQuery request, CancellationToken cancellationToken)
    {
        var listed = await catalog.ListAsync(cancellationToken);
        if (listed is not AppResult<IReadOnlyList<ProductDefinitionModel>>.Success { Data: var definitions })
            return AppResult<IReadOnlyList<ProductDto>>.Fail(((AppResult<IReadOnlyList<ProductDefinitionModel>>.Failure)listed).Error);

        var recorded = (await metadata.ListAsync(cancellationToken)).ToDictionary(row => row.Id, StringComparer.Ordinal);
        IReadOnlyList<ProductDto> products =
            [.. definitions.Select(definition => ProductMapper.Map(definition, recorded.GetValueOrDefault(definition.Slug)))];
        return AppResult<IReadOnlyList<ProductDto>>.Ok(products);
    }
}

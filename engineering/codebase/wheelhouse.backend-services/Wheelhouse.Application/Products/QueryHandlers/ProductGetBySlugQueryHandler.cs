using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Mappers;
using Wheelhouse.Application.Products.Models;
using Wheelhouse.Application.Products.Queries;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.QueryHandlers;

/// <summary>Handles <see cref="ProductGetBySlugQuery"/>.</summary>
public sealed class ProductGetBySlugQueryHandler(IProductCatalog catalog, IProductMetadataRepository metadata)
    : IQueryHandler<ProductGetBySlugQuery, AppResult<ProductDto>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductDto>> HandleAsync(ProductGetBySlugQuery request, CancellationToken cancellationToken)
    {
        var found = await catalog.FindAsync(request.Slug, cancellationToken);
        if (found is not AppResult<ProductDefinitionModel>.Success { Data: var definition })
            return AppResult<ProductDto>.Fail(((AppResult<ProductDefinitionModel>.Failure)found).Error);

        var recorded = await metadata.FindAsync(definition.Slug, cancellationToken);
        return AppResult<ProductDto>.Ok(ProductMapper.Map(definition, recorded));
    }
}

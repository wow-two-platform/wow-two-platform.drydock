using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Commands;
using Wheelhouse.Application.Products.Mappers;
using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.CommandHandlers;

/// <summary>Handles <see cref="ProductLifecycleUpdateCommand"/>; only a product the catalog defines is recorded.</summary>
public sealed class ProductLifecycleUpdateCommandHandler(IProductCatalog catalog, IProductMetadataRepository metadata)
    : ICommandHandler<ProductLifecycleUpdateCommand, AppResult<ProductDto>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductDto>> HandleAsync(
        ProductLifecycleUpdateCommand request, CancellationToken cancellationToken)
    {
        var found = await catalog.FindAsync(request.Slug, cancellationToken);
        if (found is not AppResult<ProductDefinitionModel>.Success { Data: var definition })
            return AppResult<ProductDto>.Fail(((AppResult<ProductDefinitionModel>.Failure)found).Error);

        var recorded = await metadata.SetLifecycleAsync(definition.Slug, request.Lifecycle, cancellationToken);
        return AppResult<ProductDto>.Ok(ProductMapper.Map(definition, recorded));
    }
}

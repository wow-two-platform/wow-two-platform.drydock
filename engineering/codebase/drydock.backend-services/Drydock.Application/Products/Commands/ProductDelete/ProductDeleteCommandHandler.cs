using Drydock.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductDelete;

/// <summary>Handles <see cref="ProductDeleteCommand"/>.</summary>
public sealed class ProductDeleteCommandHandler(IProductRepository store)
    : ICommandHandler<ProductDeleteCommand, AppResult<ProductDeleteResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductDeleteResult>> HandleAsync(
        ProductDeleteCommand request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return AppResult<ProductDeleteResult>.Fail(AppErrors.NotFound($"Product '{request.Id}' was not found."));

        await store.RemoveAsync(product, cancellationToken);

        return AppResult<ProductDeleteResult>.Ok(new ProductDeleteResult());
    }
}

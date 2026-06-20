using Drydock.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductDelete;

/// <summary>Handles <see cref="ProductDeleteCommand"/>.</summary>
public sealed class ProductDeleteCommandHandler(IProductStore store)
    : ICommandHandler<ProductDeleteCommand, AppResult<ProductDeleteResult.Success, ProductDeleteResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductDeleteResult.Success, ProductDeleteResult.Failure>> HandleAsync(
        ProductDeleteCommand request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return new AppResult<ProductDeleteResult.Success, ProductDeleteResult.Failure>.Failure(
                new ProductDeleteResult.Failure($"Product '{request.Id}' was not found.", FailureCategory.NotFound));

        await store.RemoveAsync(product, cancellationToken);

        return new AppResult<ProductDeleteResult.Success, ProductDeleteResult.Failure>.Success(
            new ProductDeleteResult.Success());
    }
}

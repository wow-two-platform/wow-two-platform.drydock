using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Products.Commands.ProductUpdate;

/// <summary>Handles <see cref="ProductUpdateCommand"/>.</summary>
public sealed class ProductUpdateCommandHandler(IProductRepository store, IGitHubClient gitHub)
    : ICommandHandler<ProductUpdateCommand, AppResult<ProductUpdateResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductUpdateResult>> HandleAsync(
        ProductUpdateCommand request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return AppResult<ProductUpdateResult>.Fail(AppErrors.NotFound($"Product '{request.Id}' was not found."));

        var repo = request.Repo.Trim();

        // Only spend a GitHub round-trip when the repo actually changed (format already validated).
        if (!string.Equals(repo, product.Repo, StringComparison.Ordinal))
        {
            var repoError = await ProductValidation.VerifyRepoExistsAsync(gitHub, repo, cancellationToken);
            if (repoError is { } error)
                return AppResult<ProductUpdateResult>.Fail(error);
        }

        product.Name = request.Name.Trim();
        product.Repo = repo;
        product.Status = request.Status;

        await store.UpdateAsync(product, cancellationToken);

        return AppResult<ProductUpdateResult>.Ok(new ProductUpdateResult(new ProductDto(
            product.Id, product.Slug, product.Name, product.Repo, product.Status, product.CreatedAt)));
    }
}

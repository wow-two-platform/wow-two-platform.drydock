using Drydock.Application.Abstractions;
using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductUpdate;

/// <summary>Handles <see cref="ProductUpdateCommand"/>.</summary>
public sealed class ProductUpdateCommandHandler(IProductStore store, IGitHubClient gitHub)
    : ICommandHandler<ProductUpdateCommand, AppResult<ProductUpdateResult.Success, ProductUpdateResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductUpdateResult.Success, ProductUpdateResult.Failure>> HandleAsync(
        ProductUpdateCommand request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.Id, cancellationToken);
        if (product is null)
            return Fail(FailureCategory.NotFound, $"Product '{request.Id}' was not found.");

        var repo = request.Repo.Trim();

        // Only spend a GitHub round-trip when the repo actually changed (format already validated).
        if (!string.Equals(repo, product.Repo, StringComparison.Ordinal))
        {
            var repoFailure = await ProductValidation.VerifyRepoExistsAsync(gitHub, repo, cancellationToken);
            if (repoFailure is { } rf)
                return Fail(rf.Category, rf.Message);
        }

        product.Name = request.Name.Trim();
        product.Repo = repo;
        product.Status = request.Status;

        await store.UpdateAsync(product, cancellationToken);

        return new AppResult<ProductUpdateResult.Success, ProductUpdateResult.Failure>.Success(
            new ProductUpdateResult.Success(new ProductDto(
                product.Id, product.Slug, product.Name, product.Repo, product.Status, product.CreatedAtUtc)));
    }

    private static AppResult<ProductUpdateResult.Success, ProductUpdateResult.Failure> Fail(
        FailureCategory category, string message) =>
        new AppResult<ProductUpdateResult.Success, ProductUpdateResult.Failure>.Failure(
            new ProductUpdateResult.Failure(message, category));
}

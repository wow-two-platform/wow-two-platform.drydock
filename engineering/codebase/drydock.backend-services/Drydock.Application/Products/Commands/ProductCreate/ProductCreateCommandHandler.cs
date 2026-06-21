using Drydock.Application.Abstractions;
using Drydock.Application.Products.Models;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Products.Enums;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductCreate;

/// <summary>Handles <see cref="ProductCreateCommand"/>.</summary>
public sealed class ProductCreateCommandHandler(IProductStore store, TimeProvider timeProvider, IGitHubClient gitHub)
    : ICommandHandler<ProductCreateCommand, AppResult<ProductCreateResult.Success, ProductCreateResult.Failure>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductCreateResult.Success, ProductCreateResult.Failure>> HandleAsync(
        ProductCreateCommand request, CancellationToken cancellationToken)
    {
        var repoFailure = await ProductValidation.VerifyRepoExistsAsync(gitHub, request.Repo.Trim(), cancellationToken);
        if (repoFailure is { } rf)
            return Fail(rf.Category, rf.Message);

        var slug = request.Slug.Trim();
        if (await store.ExistsBySlugAsync(slug, cancellationToken))
            return Fail(FailureCategory.Conflict, $"A product with slug '{slug}' already exists.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = request.Name.Trim(),
            Repo = request.Repo.Trim(),
            Status = ProductStatus.Draft,
            CreatedAtUtc = timeProvider.GetUtcNow()
        };

        await store.AddAsync(product, cancellationToken);

        return new AppResult<ProductCreateResult.Success, ProductCreateResult.Failure>.Success(
            new ProductCreateResult.Success(new ProductDto(
                product.Id, product.Slug, product.Name, product.Repo, product.Status, product.CreatedAtUtc)));
    }

    private static AppResult<ProductCreateResult.Success, ProductCreateResult.Failure> Fail(
        FailureCategory category, string message) =>
        new AppResult<ProductCreateResult.Success, ProductCreateResult.Failure>.Failure(
            new ProductCreateResult.Failure(message, category));
}

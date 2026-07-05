using Drydock.Application.Abstractions;
using Drydock.Application.Products.Models;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Products.Enums;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Commands.ProductCreate;

/// <summary>Handles <see cref="ProductCreateCommand"/>.</summary>
public sealed class ProductCreateCommandHandler(IProductRepository store, IGitHubClient gitHub)
    : ICommandHandler<ProductCreateCommand, AppResult<ProductCreateResult>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ProductCreateResult>> HandleAsync(
        ProductCreateCommand request, CancellationToken cancellationToken)
    {
        var repoError = await ProductValidation.VerifyRepoExistsAsync(gitHub, request.Repo.Trim(), cancellationToken);
        if (repoError is { } error)
            return AppResult<ProductCreateResult>.Fail(error);

        var slug = request.Slug.Trim();
        if (await store.ExistsBySlugAsync(slug, cancellationToken))
            return AppResult<ProductCreateResult>.Fail(AppErrors.Conflict($"A product with slug '{slug}' already exists."));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = request.Name.Trim(),
            Repo = request.Repo.Trim(),
            Status = ProductStatus.Draft
            // CreatedAt is stamped by the SDK audit interceptor on SaveChanges — not hand-set.
        };

        await store.AddAsync(product, cancellationToken);

        return AppResult<ProductCreateResult>.Ok(new ProductCreateResult(new ProductDto(
            product.Id, product.Slug, product.Name, product.Repo, product.Status, product.CreatedAt)));
    }
}

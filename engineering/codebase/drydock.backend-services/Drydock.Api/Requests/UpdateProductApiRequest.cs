using Drydock.Application.Products.Commands.ProductUpdate;
using Drydock.Domain.Products.Enums;

namespace Drydock.Api.Requests;

/// <summary>Represents the update-product request body (slug is immutable).</summary>
public sealed record UpdateProductApiRequest
{
    /// <summary>Gets the product's display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the GitHub <c>{owner}/{repo}</c> that defines the product.</summary>
    public required string Repo { get; init; }

    /// <summary>Gets the product's lifecycle state.</summary>
    public required ProductStatus Status { get; init; }
}

/// <summary>Provides mapping for <see cref="UpdateProductApiRequest"/>.</summary>
public static class UpdateProductApiRequestExtensions
{
    /// <summary>Maps the request to its <see cref="ProductUpdateCommand"/>.</summary>
    public static ProductUpdateCommand ToCommand(this UpdateProductApiRequest request, Guid id)
    {
        return new ProductUpdateCommand(id, request.Name, request.Repo, request.Status);
    }
}

using Drydock.Domain.Products.Enums;

namespace Drydock.Api.Requests;

/// <summary>Request body for updating a product (slug is immutable).</summary>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c> that defines the product.</param>
/// <param name="Status">Lifecycle state.</param>
public sealed record UpdateProductRequest(
    string Name,
    string Repo,
    ProductStatus Status);

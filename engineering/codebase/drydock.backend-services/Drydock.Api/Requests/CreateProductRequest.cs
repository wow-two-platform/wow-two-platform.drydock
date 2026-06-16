namespace Drydock.Api.Requests;

/// <summary>Request body for registering a product.</summary>
/// <param name="Slug">URL-safe slug (unique, immutable).</param>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c> that defines the product.</param>
public sealed record CreateProductRequest(
    string Slug,
    string Name,
    string Repo);

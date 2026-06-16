using Drydock.Domain.Products.Enums;

namespace Drydock.Application.Products.Models;

/// <summary>Read model for a portfolio product.</summary>
/// <param name="Id">Product id.</param>
/// <param name="Slug">URL-safe slug (unique).</param>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c> that defines the product.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="CreatedAtUtc">When the product was registered.</param>
public sealed record ProductDto(
    Guid Id,
    string Slug,
    string Name,
    string Repo,
    ProductStatus Status,
    DateTimeOffset CreatedAtUtc);

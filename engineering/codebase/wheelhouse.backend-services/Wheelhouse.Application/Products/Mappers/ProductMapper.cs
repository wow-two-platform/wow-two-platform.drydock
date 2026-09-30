using Wheelhouse.Application.Products.Models;
using Wheelhouse.Domain.Products.Entities;
using Wheelhouse.Domain.Products.Enums;

namespace Wheelhouse.Application.Products.Mappers;

/// <summary>Maps a catalog product definition and the operator's recorded metadata to the product integrations read.</summary>
internal static class ProductMapper
{
    /// <summary>Maps a definition and its recorded metadata to its projection; a product never recorded is building.</summary>
    /// <param name="definition">The product as the catalog defines it.</param>
    /// <param name="metadata">What the operator recorded about it, or <c>null</c>.</param>
    /// <returns>The product integrations read.</returns>
    public static ProductDto Map(ProductDefinitionModel definition, ProductMetadataEntity? metadata) => new()
    {
        Slug = definition.Slug,
        Name = definition.Name,
        Description = definition.Description,
        Lifecycle = metadata?.Lifecycle ?? ProductLifecycle.Building,
        Repository = new ProductRepositoryDto
        {
            Name = definition.Repository,
            Url = "https://github.com/" + definition.Repository,
            DefaultBranch = definition.DefaultBranch,
        },
        IconUrl = "/api/products/" + definition.Slug + "/icon",
        Environments = [.. definition.Environments.Select(Map)],
    };

    /// <summary>Maps an environment definition to its projection, leaving the target and server behind.</summary>
    /// <param name="environment">The environment as a fleet target binds it.</param>
    /// <returns>The environment integrations read.</returns>
    private static ProductEnvironmentDto Map(ProductEnvironmentModel environment) => new()
    {
        Name = environment.Name,
        Sites = [.. environment.Sites.Select(site => new ProductSiteDto { Name = site.Name, Url = site.Url, Exposure = site.Exposure })],
        Secrets = environment.Secrets is { } secrets
            ? new ProductSecretsDto { Vault = secrets.VaultId, Namespace = secrets.Namespace }
            : null,
    };
}

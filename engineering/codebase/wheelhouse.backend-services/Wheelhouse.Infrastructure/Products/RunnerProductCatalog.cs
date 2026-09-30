using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Infrastructure.Products;

/// <summary>Provides the code-owned product catalog through the runner, held briefly so an integration that polls
/// costs no runner process per request.</summary>
public sealed class RunnerProductCatalog(IDeploymentGateway gateway, IMemoryCache cache) : IProductCatalog
{
    /// <summary>Holds the cache key of the last catalog read.</summary>
    private const string CacheKey = "product-catalog";

    /// <summary>Holds how long a read catalog serves; a new rollout's sites show within it.</summary>
    private static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public async Task<AppResult<IReadOnlyList<ProductDefinitionModel>>> ListAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<ProductDefinitionModel>? cached) && cached is not null)
            return AppResult<IReadOnlyList<ProductDefinitionModel>>.Ok(cached);

        var read = await gateway.ReadAsync("products", null, ct);
        if (read is not AppResult<JsonElement>.Success { Data: var document })
            return AppResult<IReadOnlyList<ProductDefinitionModel>>.Fail(((AppResult<JsonElement>.Failure)read).Error);

        try
        {
            IReadOnlyList<ProductDefinitionModel> products =
                document.Deserialize<List<ProductDefinitionModel>>(JsonSerializerOptions.Web) ?? [];
            cache.Set(CacheKey, products, FreshFor);
            return AppResult<IReadOnlyList<ProductDefinitionModel>>.Ok(products);
        }
        catch (JsonException)
        {
            return AppResult<IReadOnlyList<ProductDefinitionModel>>.Fail(
                AppErrorFactory.Unexpected("The runner returned an unreadable product catalog."));
        }
    }

    /// <inheritdoc />
    public async Task<AppResult<ProductDefinitionModel>> FindAsync(string slug, CancellationToken ct)
    {
        var listed = await ListAsync(ct);
        if (listed is not AppResult<IReadOnlyList<ProductDefinitionModel>>.Success { Data: var products })
            return AppResult<ProductDefinitionModel>.Fail(((AppResult<IReadOnlyList<ProductDefinitionModel>>.Failure)listed).Error);

        var product = products.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.Ordinal));
        return product is null
            ? AppResult<ProductDefinitionModel>.Fail(AppErrorFactory.NotFound($"Product '{slug}' is not in the catalog."))
            : AppResult<ProductDefinitionModel>.Ok(product);
    }
}

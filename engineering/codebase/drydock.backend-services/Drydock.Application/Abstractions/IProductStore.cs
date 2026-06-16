using Drydock.Domain.Products.Entities;

namespace Drydock.Application.Abstractions;

/// <summary>Persistence gateway for <see cref="Product"/> aggregates — keeps the application layer off EF Core.</summary>
public interface IProductStore
{
    /// <summary>Adds a new product and returns it.</summary>
    Task<Product> AddAsync(Product product, CancellationToken ct = default);

    /// <summary>Lists all registered products, most-recently-created first.</summary>
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default);

    /// <summary>Finds a product by id, or <see langword="null"/> if none exists.</summary>
    Task<Product?> FindAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persists changes to an already-tracked product.</summary>
    Task UpdateAsync(Product product, CancellationToken ct = default);

    /// <summary>Removes a product.</summary>
    Task RemoveAsync(Product product, CancellationToken ct = default);

    /// <summary>Returns <see langword="true"/> if a product with the given slug already exists.</summary>
    Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default);
}

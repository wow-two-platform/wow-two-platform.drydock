using Drydock.Application.Abstractions;
using Drydock.Domain.Products.Entities;
using Microsoft.EntityFrameworkCore;

namespace Drydock.Persistence.Stores;

/// <summary>EF Core implementation of <see cref="IProductStore"/>.</summary>
internal sealed class EfProductStore(DrydockDbContext db) : IProductStore
{
    /// <inheritdoc />
    public async Task<Product> AddAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default) =>
        await db.Products.AsNoTracking().OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct);

    /// <inheritdoc />
    public async Task<Product?> FindAsync(Guid id, CancellationToken ct = default) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    /// <inheritdoc />
    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Update(product);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Product product, CancellationToken ct = default)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default) =>
        await db.Products.AnyAsync(p => p.Slug == slug, ct);
}

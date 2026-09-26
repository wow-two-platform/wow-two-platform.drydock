using Wheelhouse.Application.Abstractions;
using Wheelhouse.Domain.Products.Entities;
using Microsoft.EntityFrameworkCore;

namespace Wheelhouse.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IProductRepository"/>.</summary>
internal sealed class EfProductRepository(WheelhouseDbContext db) : IProductRepository
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
        await db.Products.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

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

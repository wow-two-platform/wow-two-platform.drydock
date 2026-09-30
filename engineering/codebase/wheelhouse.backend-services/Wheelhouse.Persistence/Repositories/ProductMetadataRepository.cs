using Microsoft.EntityFrameworkCore;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Domain.Products.Entities;
using Wheelhouse.Domain.Products.Enums;

namespace Wheelhouse.Persistence.Repositories;

/// <summary>Accesses what the operator records about catalog products.</summary>
internal sealed class ProductMetadataRepository(WheelhouseDbContext db) : IProductMetadataRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductMetadataEntity>> ListAsync(CancellationToken ct = default) =>
        await db.ProductMetadata.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc />
    public async Task<ProductMetadataEntity?> FindAsync(string slug, CancellationToken ct = default) =>
        await db.ProductMetadata.AsNoTracking().FirstOrDefaultAsync(row => row.Id == slug, ct);

    /// <inheritdoc />
    public async Task<ProductMetadataEntity> SetLifecycleAsync(
        string slug, ProductLifecycle lifecycle, CancellationToken ct = default)
    {
        var row = await db.ProductMetadata.FirstOrDefaultAsync(item => item.Id == slug, ct);
        if (row is null)
            db.ProductMetadata.Add(row = new ProductMetadataEntity { Id = slug, Lifecycle = lifecycle });
        else
            row.Lifecycle = lifecycle;

        await db.SaveChangesAsync(ct);
        return row;
    }
}

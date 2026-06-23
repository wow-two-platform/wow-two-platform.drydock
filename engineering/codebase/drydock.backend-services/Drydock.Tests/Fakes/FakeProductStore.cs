using Drydock.Application.Abstractions;
using Drydock.Domain.Products.Entities;

namespace Drydock.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IProductStore"/> for the unit tier. The version-status state machine only reads
/// via <see cref="FindAsync"/>; the write/list members throw so an accidental call surfaces loudly.
/// </summary>
internal sealed class FakeProductStore(params Product[] products) : IProductStore
{
    private readonly Dictionary<Guid, Product> _byId = products.ToDictionary(p => p.Id);

    public Task<Product?> FindAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_byId.GetValueOrDefault(id));

    public Task<Product> AddAsync(Product product, CancellationToken ct = default) =>
        throw new NotSupportedException("Unit-tier fake: AddAsync is out of scope for the version-status query.");

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default) =>
        throw new NotSupportedException("Unit-tier fake: ListAsync is out of scope for the version-status query.");

    public Task UpdateAsync(Product product, CancellationToken ct = default) =>
        throw new NotSupportedException("Unit-tier fake: UpdateAsync is out of scope for the version-status query.");

    public Task RemoveAsync(Product product, CancellationToken ct = default) =>
        throw new NotSupportedException("Unit-tier fake: RemoveAsync is out of scope for the version-status query.");

    public Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default) =>
        throw new NotSupportedException("Unit-tier fake: ExistsBySlugAsync is out of scope for the version-status query.");
}

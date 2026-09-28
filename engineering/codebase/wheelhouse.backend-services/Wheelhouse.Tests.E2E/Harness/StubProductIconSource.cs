using Wheelhouse.Application.Abstractions;

namespace Wheelhouse.Tests.E2E.Harness;

/// <summary>Serves icons from a dictionary keyed by repository, so no test reads GitHub.</summary>
public sealed class StubProductIconSource : IProductIconSource
{
    /// <summary>The icon each repository returns; a repository missing here carries none.</summary>
    public Dictionary<string, ProductIconImage> Icons { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<ProductIconImage?> FindAsync(string repository, CancellationToken ct) =>
        Task.FromResult(Icons.TryGetValue(repository, out var icon) ? icon : null);
}

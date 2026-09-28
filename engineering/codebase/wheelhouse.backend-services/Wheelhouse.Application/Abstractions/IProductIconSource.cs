namespace Wheelhouse.Application.Abstractions;

/// <summary>A product's own icon, found in its source repository.</summary>
/// <param name="Content">The image bytes.</param>
/// <param name="ContentType">The image media type.</param>
/// <param name="Path">The repository path the icon was read from.</param>
public sealed record ProductIconImage(byte[] Content, string ContentType, string Path);

/// <summary>Finds the icon a product's repository carries, so every product shows its own mark.</summary>
public interface IProductIconSource
{
    /// <summary>Returns the repository's icon, or <see langword="null"/> when it carries none or cannot be read.</summary>
    /// <param name="repository">The <c>{owner}/{repo}</c> reference.</param>
    /// <param name="ct">A cancellation token.</param>
    Task<ProductIconImage?> FindAsync(string repository, CancellationToken ct);
}

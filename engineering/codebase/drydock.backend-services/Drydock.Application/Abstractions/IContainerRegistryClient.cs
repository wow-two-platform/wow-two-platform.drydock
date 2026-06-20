namespace Drydock.Application.Abstractions;

/// <summary>
/// Outcome of probing whether a container image tag exists in the registry — lets a handler tell a
/// genuinely-missing image from one it isn't authorized to see (so it can flag "not built" vs "re-authorize").
/// </summary>
public enum ImageCheck
{
    /// <summary>The image tag exists in the registry → the manifest request returned 200.</summary>
    Exists,

    /// <summary>No such image tag → the manifest request returned 404.</summary>
    Missing,

    /// <summary>The registry refused the request — neither anonymous nor the admin token could read it → 401/403.</summary>
    Unauthorized,

    /// <summary>The check could not be completed (transport error or an unexpected registry status).</summary>
    Failed
}

/// <summary>Reads image metadata from a container registry to confirm a published, deployable image exists.</summary>
public interface IContainerRegistryClient
{
    /// <summary>
    /// Probes whether the image for <paramref name="repo"/> at <paramref name="tag"/> (i.e.
    /// <c>ghcr.io/{owner}/{repo}:{tag}</c>) is published in the registry.
    /// </summary>
    /// <param name="repo">The <c>{owner}/{repo}</c> reference the image is named after.</param>
    /// <param name="tag">The image tag to probe.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An <see cref="ImageCheck"/> categorizing the outcome.</returns>
    Task<ImageCheck> ImageExistsAsync(string repo, string tag, CancellationToken ct);
}

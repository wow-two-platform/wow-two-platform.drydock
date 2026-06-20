namespace Drydock.Application.Products.Models;

/// <summary>Defines whether a product has a ready, deployable image — the resolved build/image status.</summary>
/// <example>Used to badge a product row in the dashboard and to gate a deploy.</example>
public enum ProductVersionState
{
    /// <summary>The repo has no image-publishing workflow → nothing builds images for it.</summary>
    NoCi,

    /// <summary>The repo can publish images but has no release and no published image at all.</summary>
    NeverBuilt,

    /// <summary>A <c>:latest</c> image is published but no release has been cut — a built, unversioned HEAD build.</summary>
    UnreleasedBuild,

    /// <summary>The latest release has no image yet and its publish build is still running.</summary>
    BuildPending,

    /// <summary>The latest release has no image and its publish build failed.</summary>
    BuildFailed,

    /// <summary>The latest release's image is published and ready to deploy.</summary>
    Ready,

    /// <summary>The latest release has no image yet, but an older release does — the newest deployable lags behind.</summary>
    LatestNotReady,

    /// <summary>The status could not be determined — a GitHub or registry probe was unauthorized or failed.</summary>
    Unknown
}

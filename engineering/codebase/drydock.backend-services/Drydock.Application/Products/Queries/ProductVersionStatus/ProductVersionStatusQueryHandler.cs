using Drydock.Application.Abstractions;
using Drydock.Application.Products.Models;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Application.Products.Queries.ProductVersionStatus;

using AppOutcome = AppResult<ProductVersionStatusResult.Success, ProductVersionStatusResult.Failure>;

/// <summary>Handles <see cref="ProductVersionStatusQuery"/>.</summary>
public sealed class ProductVersionStatusQueryHandler(
    IProductStore store,
    IGitHubClient gitHub,
    IContainerRegistryClient registry)
    : IQueryHandler<ProductVersionStatusQuery, AppOutcome>
{
    /// <summary>How many recent releases to scan when the latest has no published image.</summary>
    private const int ReleaseScanLimit = 5;

    /// <summary>The floating tag the publish workflow pushes on a manual (no-release) dispatch.</summary>
    private const string LatestTag = "latest";

    /// <summary>The publish-image workflow path that marks a repo as image-producing.</summary>
    private const string PublishWorkflowPath = ".github/workflows/publish-docker-image.yml";

    /// <summary>The publish-image workflow file name, used to query its runs via the Actions API.</summary>
    private const string PublishWorkflowFile = "publish-docker-image.yml";

    /// <inheritdoc />
    public async ValueTask<AppOutcome> HandleAsync(ProductVersionStatusQuery request, CancellationToken cancellationToken)
    {
        var product = await store.FindAsync(request.ProductId, cancellationToken);
        if (product is null)
            return Fail(FailureCategory.NotFound, $"Product '{request.ProductId}' was not found.");

        var repo = product.Repo;

        // No image-publishing workflow → nothing builds images for this repo.
        var marker = await gitHub.FileExistsAsync(repo, PublishWorkflowPath, cancellationToken);
        if (marker is FileCheck.Unauthorized or FileCheck.Failed)
            return Ok(Unknown(GitHubReason(marker is FileCheck.Unauthorized)));
        if (marker is FileCheck.Absent)
            return Ok(NoCi());

        // Resolve the latest released version — the version we'd want to deploy.
        var latest = await gitHub.GetLatestReleaseAsync(repo, cancellationToken);
        if (latest.Outcome is ReleaseLookup.Unauthorized or ReleaseLookup.Failed)
            return Ok(Unknown(GitHubReason(latest.Outcome is ReleaseLookup.Unauthorized)));
        if (latest.Outcome is ReleaseLookup.None || latest.Releases.Count == 0)
            return Ok(await ResolveUnreleasedAsync(repo, cancellationToken));

        var latestRelease = latest.Releases[0];

        // Is the latest release's image already published?
        var latestImage = await registry.ImageExistsAsync(repo, latestRelease.Tag, cancellationToken);
        if (latestImage is ImageCheck.Unauthorized or ImageCheck.Failed)
            return Ok(Unknown(RegistryReason(latestImage is ImageCheck.Unauthorized)));
        if (latestImage is ImageCheck.Exists)
            return Ok(Ready(repo, latestRelease, latestRelease));

        // Latest has no image yet — scan older releases for the newest one that does.
        return Ok(await ResolveOlderReadyAsync(repo, latestRelease, cancellationToken));
    }

    /// <summary>No release exists — a manual <c>:latest</c> build may still be published.</summary>
    private async Task<ProductVersionDto> ResolveUnreleasedAsync(string repo, CancellationToken cancellationToken)
    {
        var image = await registry.ImageExistsAsync(repo, LatestTag, cancellationToken);
        if (image is ImageCheck.Unauthorized or ImageCheck.Failed)
            return Unknown(RegistryReason(image is ImageCheck.Unauthorized));
        if (image is ImageCheck.Exists)
            return UnreleasedBuild(repo);

        return NeverBuilt(null, null, "This repository has published no releases or images yet.");
    }

    private async Task<ProductVersionDto> ResolveOlderReadyAsync(
        string repo, ReleaseInfo latestRelease, CancellationToken cancellationToken)
    {
        var releases = await gitHub.GetReleasesAsync(repo, ReleaseScanLimit, cancellationToken);
        if (releases.Outcome is ReleaseLookup.Unauthorized or ReleaseLookup.Failed)
            return Unknown(GitHubReason(releases.Outcome is ReleaseLookup.Unauthorized));

        // Walk newest→older, skipping the latest tag (already known to have no image).
        foreach (var release in releases.Releases)
        {
            if (release.Tag == latestRelease.Tag)
                continue;

            var image = await registry.ImageExistsAsync(repo, release.Tag, cancellationToken);
            if (image is ImageCheck.Unauthorized or ImageCheck.Failed)
                return Unknown(RegistryReason(image is ImageCheck.Unauthorized));
            if (image is ImageCheck.Exists)
                return LatestNotReady(repo, latestRelease, release);
        }

        // Releases exist but none carries an image — the build is pending, failed, or never ran.
        return await ResolveBuildStatusAsync(repo, latestRelease, cancellationToken);
    }

    /// <summary>The latest release has no image — read the publish run to tell pending from failed from never-ran.</summary>
    private async Task<ProductVersionDto> ResolveBuildStatusAsync(
        string repo, ReleaseInfo latestRelease, CancellationToken cancellationToken)
    {
        var run = await gitHub.GetLatestWorkflowRunAsync(repo, PublishWorkflowFile, cancellationToken);
        switch (run)
        {
            case BuildRunCheck.Running:
                return BuildPending(latestRelease, $"The image for {latestRelease.Tag} is still building.");
            case BuildRunCheck.Succeeded:
                return BuildPending(latestRelease, $"The build for {latestRelease.Tag} succeeded but its image isn't in the registry yet.");
            case BuildRunCheck.Failed:
                return BuildFailed(latestRelease, $"The build for {latestRelease.Tag} failed — check the publish-docker-image run.");
            case BuildRunCheck.None:
                return NeverBuilt(latestRelease.Tag, latestRelease.PublishedAtUtc, $"Release {latestRelease.Tag} is published but no build has run for it.");
            case BuildRunCheck.Unauthorized:
                return Unknown(GitHubReason(unauthorized: true));
            default:
                return Unknown(GitHubReason(unauthorized: false));
        }
    }

    private static ProductVersionDto Ready(string repo, ReleaseInfo latest, ReleaseInfo ready)
    {
        return new ProductVersionDto(
            ProductVersionState.Ready,
            latest.Tag, latest.PublishedAtUtc,
            ready.Tag, ready.PublishedAtUtc,
            ImageRef(repo, ready.Tag),
            $"Release {ready.Tag}'s image is published and ready to deploy.");
    }

    private static ProductVersionDto LatestNotReady(string repo, ReleaseInfo latest, ReleaseInfo ready)
    {
        return new ProductVersionDto(
            ProductVersionState.LatestNotReady,
            latest.Tag, latest.PublishedAtUtc,
            ready.Tag, ready.PublishedAtUtc,
            ImageRef(repo, ready.Tag),
            $"Latest release {latest.Tag} has no image yet; the newest deployable is {ready.Tag}.");
    }

    private static ProductVersionDto UnreleasedBuild(string repo)
    {
        return new ProductVersionDto(
            ProductVersionState.UnreleasedBuild,
            null, null, null, null,
            ImageRef(repo, LatestTag),
            "A :latest image is published but no release has been cut. Cut a vX.Y.Z release to make it a deployable version.");
    }

    private static ProductVersionDto BuildPending(ReleaseInfo latest, string detail)
    {
        return new ProductVersionDto(ProductVersionState.BuildPending, latest.Tag, latest.PublishedAtUtc, null, null, null, detail);
    }

    private static ProductVersionDto BuildFailed(ReleaseInfo latest, string detail)
    {
        return new ProductVersionDto(ProductVersionState.BuildFailed, latest.Tag, latest.PublishedAtUtc, null, null, null, detail);
    }

    private static ProductVersionDto NeverBuilt(string? latestTag, DateTimeOffset? latestAtUtc, string detail)
    {
        return new ProductVersionDto(ProductVersionState.NeverBuilt, latestTag, latestAtUtc, null, null, null, detail);
    }

    private static ProductVersionDto NoCi()
    {
        return new ProductVersionDto(
            ProductVersionState.NoCi, null, null, null, null, null,
            "No image-publishing workflow in this repository, so nothing builds its images.");
    }

    private static ProductVersionDto Unknown(string detail)
    {
        return new ProductVersionDto(ProductVersionState.Unknown, null, null, null, null, null, detail);
    }

    /// <summary>Maps a GitHub probe failure to an actionable reason — unauthorized (re-authorize) vs a transport error.</summary>
    private static string GitHubReason(bool unauthorized)
    {
        return unauthorized
            ? "GitHub denied the check — the sign-in may not have access to this repository. Re-authorize and retry."
            : "Couldn't reach GitHub to check this repository. Try again shortly.";
    }

    /// <summary>Maps a registry probe failure to an actionable reason — unauthorized (missing scope) vs a transport error.</summary>
    private static string RegistryReason(bool unauthorized)
    {
        return unauthorized
            ? "The container registry denied the image check — the token may need the read:packages scope."
            : "Couldn't reach the container registry. Try again shortly.";
    }

    private static string ImageRef(string repo, string tag)
    {
        return $"ghcr.io/{repo}:{tag}";
    }

    private static AppOutcome Ok(ProductVersionDto version)
    {
        return new AppOutcome.Success(new ProductVersionStatusResult.Success(version));
    }

    private static AppOutcome Fail(FailureCategory category, string message)
    {
        return new AppOutcome.Failure(new ProductVersionStatusResult.Failure(message, category));
    }
}

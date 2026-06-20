namespace Drydock.Application.Abstractions;

/// <summary>
/// Outcome of a repository-existence probe against GitHub — lets a handler distinguish a
/// genuinely missing repo from a token that can't see it (so it can message "not found" vs
/// "re-authorize for private repos").
/// </summary>
public enum RepoCheck
{
    /// <summary>The repository exists and is visible to the current token → GitHub returned 200.</summary>
    Exists,

    /// <summary>No such repository, or it's private and the token can't see it → GitHub returned 404.</summary>
    NotFound,

    /// <summary>The token is missing/expired or lacks the <c>repo</c> scope → GitHub returned 401/403.</summary>
    Unauthorized,

    /// <summary>The check could not be completed (transport error or an unexpected GitHub status).</summary>
    Failed
}

/// <summary>
/// Outcome of probing for the publish-workflow marker (<c>.github/workflows/publish-docker-image.yml</c>)
/// — tells a handler whether the repo is wired to publish images at all.
/// </summary>
public enum MarkerCheck
{
    /// <summary>The workflow file exists → the repo is set up to publish images.</summary>
    Present,

    /// <summary>No such workflow file → the repo has no image-publishing CI.</summary>
    Absent,

    /// <summary>The token is missing/expired or can't see the repo → GitHub returned 401/403.</summary>
    Unauthorized,

    /// <summary>The check could not be completed (transport error or an unexpected GitHub status).</summary>
    Failed
}

/// <summary>
/// Outcome of resolving releases from GitHub — distinguishes a repo that has never released
/// (no tags yet) from a token that can't see it or a transport failure.
/// </summary>
public enum ReleaseLookup
{
    /// <summary>One or more releases were resolved → <see cref="ReleaseList.Releases"/> is populated.</summary>
    Found,

    /// <summary>The repo exists but has published no releases yet → GitHub returned 404 on the releases endpoint.</summary>
    None,

    /// <summary>The token is missing/expired or can't see the repo → GitHub returned 401/403.</summary>
    Unauthorized,

    /// <summary>The lookup could not be completed (transport error or an unexpected GitHub status).</summary>
    Failed
}

/// <summary>Represents a single published GitHub release — the bits Drydock keys a deployable version on.</summary>
/// <param name="Tag">The release tag (e.g. <c>v1.2.0</c>) — the image tag is derived from it.</param>
/// <param name="PublishedAtUtc">When the release was published, when GitHub reports it.</param>
public sealed record ReleaseInfo(string Tag, DateTimeOffset? PublishedAtUtc);

/// <summary>Represents the outcome of a releases lookup — the categorized result plus any resolved releases.</summary>
/// <param name="Outcome">How the lookup resolved.</param>
/// <param name="Releases">The releases, newest first; empty unless <paramref name="Outcome"/> is <see cref="ReleaseLookup.Found"/>.</param>
public sealed record ReleaseList(ReleaseLookup Outcome, IReadOnlyList<ReleaseInfo> Releases)
{
    /// <summary>Gets an empty list result for the given non-<see cref="ReleaseLookup.Found"/> outcome.</summary>
    /// <param name="outcome">The categorized lookup outcome.</param>
    /// <returns>A <see cref="ReleaseList"/> carrying no releases.</returns>
    public static ReleaseList Empty(ReleaseLookup outcome)
    {
        return new ReleaseList(outcome, []);
    }
}

/// <summary>
/// Outcome of reading the latest publish-image workflow run — distinguishes a build that succeeded,
/// failed, is still running, never ran, or couldn't be read.
/// </summary>
public enum BuildRunCheck
{
    /// <summary>The latest publish-image run completed successfully.</summary>
    Succeeded,

    /// <summary>The latest publish-image run completed unsuccessfully (failed, cancelled, or timed out).</summary>
    Failed,

    /// <summary>A publish-image run is queued or in progress.</summary>
    Running,

    /// <summary>The publish-image workflow has no runs yet.</summary>
    None,

    /// <summary>The token is missing/expired or can't see the repo → GitHub returned 401/403.</summary>
    Unauthorized,

    /// <summary>The check could not be completed (transport error or an unexpected GitHub status).</summary>
    ProbeFailed
}

/// <summary>Reads repository metadata from GitHub on behalf of the signed-in admin.</summary>
public interface IGitHubClient
{
    /// <summary>
    /// Probes whether <paramref name="repo"/> (an <c>{owner}/{repo}</c> reference) exists and is
    /// visible to the current request's GitHub token.
    /// </summary>
    /// <param name="repo">The <c>{owner}/{repo}</c> reference to check.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="RepoCheck"/> categorizing the outcome.</returns>
    Task<RepoCheck> RepoExistsAsync(string repo, CancellationToken ct);

    /// <summary>
    /// Probes whether <paramref name="repo"/> carries the publish-Docker-image workflow
    /// (<c>.github/workflows/publish-docker-image.yml</c>) — the marker that it builds images at all.
    /// </summary>
    /// <param name="repo">The <c>{owner}/{repo}</c> reference to check.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="MarkerCheck"/> categorizing the outcome.</returns>
    Task<MarkerCheck> PublishWorkflowExistsAsync(string repo, CancellationToken ct);

    /// <summary>Gets the latest published release of <paramref name="repo"/>.</summary>
    /// <param name="repo">The <c>{owner}/{repo}</c> reference to read.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ReleaseList"/> whose <see cref="ReleaseList.Releases"/> holds the single latest release when found.</returns>
    Task<ReleaseList> GetLatestReleaseAsync(string repo, CancellationToken ct);

    /// <summary>Gets up to <paramref name="limit"/> of <paramref name="repo"/>'s releases, newest first.</summary>
    /// <param name="repo">The <c>{owner}/{repo}</c> reference to read.</param>
    /// <param name="limit">The maximum number of releases to return.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="ReleaseList"/> carrying the resolved releases, newest first.</returns>
    Task<ReleaseList> GetReleasesAsync(string repo, int limit, CancellationToken ct);

    /// <summary>Reads the conclusion of the latest publish-image workflow run for <paramref name="repo"/>.</summary>
    /// <param name="repo">The <c>{owner}/{repo}</c> reference to read.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A <see cref="BuildRunCheck"/> categorizing the latest run.</returns>
    Task<BuildRunCheck> GetLatestPublishRunAsync(string repo, CancellationToken ct);
}

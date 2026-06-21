using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;

namespace Drydock.IntegrationTests.Harness;

/// <summary>
/// Test double for <see cref="IGitHubClient"/> — short-circuits every GitHub probe so the Products vertical
/// never calls the real GitHub REST API (which would also need the signed-in admin's OAuth token).
/// </summary>
/// <remarks>Defaults to the happy path (<see cref="RepoCheck.Exists"/>, file present, no releases). Set the
/// properties to drive the NotFound / Unauthorized / no-CI / never-built / latest-not-ready branches in a test.</remarks>
public sealed class StubGitHubClient : IGitHubClient
{
    /// <summary>The canned outcome every <see cref="RepoExistsAsync"/> call returns.</summary>
    public RepoCheck Result { get; set; } = RepoCheck.Exists;

    /// <summary>The canned outcome every <see cref="FileExistsAsync"/> call returns.</summary>
    public FileCheck Marker { get; set; } = FileCheck.Present;

    /// <summary>The releases the lookups return, newest first. Drives both the latest and the list calls.</summary>
    public IReadOnlyList<ReleaseInfo> Releases { get; set; } = [];

    /// <summary>The outcome the lookups report when <see cref="Releases"/> is populated (override for failure paths).</summary>
    public ReleaseLookup ReleaseOutcome { get; set; } = ReleaseLookup.Found;

    /// <summary>The canned outcome every <see cref="GetLatestWorkflowRunAsync"/> call returns.</summary>
    public BuildRunCheck PublishRun { get; set; } = BuildRunCheck.None;

    /// <inheritdoc />
    public Task<RepoCheck> RepoExistsAsync(string repo, CancellationToken ct) => Task.FromResult(Result);

    /// <inheritdoc />
    public Task<FileCheck> FileExistsAsync(string repo, string path, CancellationToken ct) => Task.FromResult(Marker);

    /// <inheritdoc />
    public Task<ReleaseList> GetLatestReleaseAsync(string repo, CancellationToken ct)
    {
        if (ReleaseOutcome is not ReleaseLookup.Found)
            return Task.FromResult(ReleaseList.Empty(ReleaseOutcome));
        if (Releases.Count == 0)
            return Task.FromResult(ReleaseList.Empty(ReleaseLookup.None));

        return Task.FromResult(new ReleaseList(ReleaseLookup.Found, [Releases[0]]));
    }

    /// <inheritdoc />
    public Task<ReleaseList> GetReleasesAsync(string repo, int limit, CancellationToken ct)
    {
        if (ReleaseOutcome is not ReleaseLookup.Found)
            return Task.FromResult(ReleaseList.Empty(ReleaseOutcome));
        if (Releases.Count == 0)
            return Task.FromResult(ReleaseList.Empty(ReleaseLookup.None));

        var page = Releases.Take(limit).ToList();
        return Task.FromResult(new ReleaseList(ReleaseLookup.Found, page));
    }

    /// <inheritdoc />
    public Task<BuildRunCheck> GetLatestWorkflowRunAsync(string repo, string workflowFile, CancellationToken ct) => Task.FromResult(PublishRun);
}

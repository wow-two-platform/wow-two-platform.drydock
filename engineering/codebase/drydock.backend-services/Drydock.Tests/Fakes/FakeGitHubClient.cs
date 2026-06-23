using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;

namespace Drydock.Tests.Fakes;

/// <summary>
/// Configurable, in-memory <see cref="IGitHubClient"/> for the unit tier — every probe returns a canned
/// value, so the version-status state machine runs with no network. Local to this project on purpose
/// (the e2e <c>StubGitHubClient</c> is not referenced).
/// </summary>
/// <remarks>Defaults to the happy path: repo exists, the publish-workflow marker is present, the release
/// lookup "found" but with an empty set. Set the properties to drive each branch.</remarks>
internal sealed class FakeGitHubClient : IGitHubClient
{
    /// <summary>Canned outcome for <see cref="RepoExistsAsync"/>.</summary>
    public RepoCheck Repo { get; set; } = RepoCheck.Exists;

    /// <summary>Canned outcome for <see cref="FileExistsAsync"/> (the publish-workflow marker probe).</summary>
    public FileCheck Marker { get; set; } = FileCheck.Present;

    /// <summary>The releases the lookups return, newest first. Drives both the latest and the list calls.</summary>
    public IReadOnlyList<ReleaseInfo> Releases { get; set; } = [];

    /// <summary>The outcome the lookups report (override for the Unauthorized / Failed paths).</summary>
    public ReleaseLookup ReleaseOutcome { get; set; } = ReleaseLookup.Found;

    /// <summary>Canned outcome for <see cref="GetLatestWorkflowRunAsync"/> (the publish-run probe).</summary>
    public BuildRunCheck PublishRun { get; set; } = BuildRunCheck.None;

    /// <summary>Records the limit the handler asked <see cref="GetReleasesAsync"/> for — lets a test assert the scan window.</summary>
    public int? LastReleasesLimit { get; private set; }

    public Task<RepoCheck> RepoExistsAsync(string repo, CancellationToken ct) => Task.FromResult(Repo);

    public Task<FileCheck> FileExistsAsync(string repo, string path, CancellationToken ct) => Task.FromResult(Marker);

    public Task<ReleaseList> GetLatestReleaseAsync(string repo, CancellationToken ct)
    {
        if (ReleaseOutcome is not ReleaseLookup.Found)
            return Task.FromResult(ReleaseList.Empty(ReleaseOutcome));
        if (Releases.Count == 0)
            return Task.FromResult(ReleaseList.Empty(ReleaseLookup.None));

        return Task.FromResult(new ReleaseList(ReleaseLookup.Found, [Releases[0]]));
    }

    public Task<ReleaseList> GetReleasesAsync(string repo, int limit, CancellationToken ct)
    {
        LastReleasesLimit = limit;

        if (ReleaseOutcome is not ReleaseLookup.Found)
            return Task.FromResult(ReleaseList.Empty(ReleaseOutcome));
        if (Releases.Count == 0)
            return Task.FromResult(ReleaseList.Empty(ReleaseLookup.None));

        var page = Releases.Take(limit).ToList();
        return Task.FromResult(new ReleaseList(ReleaseLookup.Found, page));
    }

    public Task<BuildRunCheck> GetLatestWorkflowRunAsync(string repo, string workflowFile, CancellationToken ct) =>
        Task.FromResult(PublishRun);
}

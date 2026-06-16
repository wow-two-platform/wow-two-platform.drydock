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
}

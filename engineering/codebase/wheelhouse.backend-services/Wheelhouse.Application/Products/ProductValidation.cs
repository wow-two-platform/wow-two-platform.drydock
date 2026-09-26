using WoW.Two.Sdk.Backend.Beta.Foundation.Errors;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;

namespace Wheelhouse.Application.Products;

/// <summary>Shared input checks for the Products vertical — keeps Create and Update rules in one place.</summary>
internal static class ProductValidation
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="repo"/> is a valid GitHub
    /// <c>{owner}/{repo}</c> reference — exactly one slash, both parts non-empty, no whitespace,
    /// and no URL scheme.
    /// </summary>
    public static bool IsValidRepo(string repo)
    {
        if (string.IsNullOrWhiteSpace(repo))
            return false;
        if (repo.Contains("://", StringComparison.Ordinal))
            return false;
        if (repo.Any(char.IsWhiteSpace))
            return false;

        var parts = repo.Split('/');
        return parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0;
    }

    /// <summary>
    /// Verifies the repo exists and is visible to the signed-in admin. Call only after <see cref="IsValidRepo"/>
    /// passes (format is the cheap gate before the network call).
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when the repo exists; otherwise the <see cref="AppError"/> the handler returns
    /// as its operation failure.
    /// </returns>
    public static async Task<AppError?> VerifyRepoExistsAsync(
        IGitHubClient gitHub, string repo, CancellationToken ct)
    {
        var check = await gitHub.RepoExistsAsync(repo, ct);
        return check switch
        {
            RepoCheck.Exists => null,
            RepoCheck.NotFound => AppErrors.NotFound($"Repository '{repo}' not found or not accessible."),
            RepoCheck.Unauthorized => AppErrors.Unauthorized("Re-authorize Wheelhouse to access private repositories."),
            _ => AppErrors.Unexpected($"Could not verify repository '{repo}' with GitHub. Try again.")
        };
    }
}

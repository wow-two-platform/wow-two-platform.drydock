using System.Net;
using System.Net.Http.Headers;
using Drydock.Application.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Drydock.Infrastructure.GitHub;

/// <summary>
/// Typed <see cref="HttpClient"/> adapter over the GitHub REST API. Authorizes each call with the
/// signed-in admin's OAuth token (lifted from the current request via <see cref="IHttpContextAccessor"/>),
/// so repo visibility matches what that user can see — public and private.
/// </summary>
internal sealed class GitHubClient(
    HttpClient http,
    IHttpContextAccessor httpContextAccessor,
    ILogger<GitHubClient> logger) : IGitHubClient
{
    /// <inheritdoc />
    public async Task<RepoCheck> RepoExistsAsync(string repo, CancellationToken ct)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            logger.LogWarning("GitHub repo check requested with no active HTTP context.");
            return RepoCheck.Failed;
        }

        // The token is saved on the auth ticket (SaveTokens = true on the GitHub handler).
        var token = await context.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
            return RepoCheck.Unauthorized;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{repo}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "GitHub repo check for {Repo} failed to reach the API.", repo);
            return RepoCheck.Failed;
        }

        using (response)
        {
            return response.StatusCode switch
            {
                HttpStatusCode.OK => RepoCheck.Exists,
                HttpStatusCode.NotFound => RepoCheck.NotFound,
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => RepoCheck.Unauthorized,
                _ => Unexpected(repo, response.StatusCode)
            };
        }
    }

    private RepoCheck Unexpected(string repo, HttpStatusCode status)
    {
        logger.LogWarning("GitHub repo check for {Repo} returned an unexpected status {Status}.", repo, status);
        return RepoCheck.Failed;
    }
}

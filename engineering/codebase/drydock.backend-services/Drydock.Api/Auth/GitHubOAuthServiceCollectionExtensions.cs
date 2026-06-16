using AspNet.Security.OAuth.GitHub;
using Microsoft.AspNetCore.Authentication;

namespace Drydock.Api.Auth;

// ── v0.1 / v0.2 migration note ───────────────────────────────────────────────
// This file is an INLINE MIRROR of the beta SDK's
//   WoW.Two.Sdk.Backend.Beta.Identity.OAuth.GitHub.GitHubOAuthServiceCollectionExtensions
// It wraps `AspNet.Security.OAuth.GitHub`'s `.AddGitHub(...)` with the SAME signature
// (clientId, clientSecret, params scopes) so v0.2 can delete this file, reference the SDK
// package, and swap the call site with no behaviour change.
//
// SCOPE-EXPANSION PATH (the reason GitHub was chosen over Google):
//   v0.1  → "user:email"                       (identity only — gate the dashboard)
//   now   → + "repo"                            (same OAuth token reads public + private repos —
//                                                 powers product repo-existence verification)
//   later → + "read:packages"                   (pull images from GHCR for deploys)
// Widen the `scopes` passed at the call site; nothing here changes. NOTE: widening a granted
// scope needs re-consent — the admin signs out/in to mint a token carrying the new scope.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>GitHub OAuth provider — inline mirror of the beta SDK extension (swap-in target for v0.2).</summary>
internal static class GitHubOAuthServiceCollectionExtensions
{
    /// <summary>Registers GitHub as an OAuth provider on the authentication builder.</summary>
    public static AuthenticationBuilder AddGitHubAuthentication(
        this AuthenticationBuilder auth,
        string clientId,
        string clientSecret,
        Action<GitHubAuthenticationOptions>? configure = null,
        params string[] scopes)
    {
        ArgumentNullException.ThrowIfNull(auth);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);

        return auth.AddGitHub(options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.SaveTokens = true;
            foreach (var scope in scopes)
                options.Scope.Add(scope);

            configure?.Invoke(options);
        });
    }
}

using Drydock.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using WoW.Two.Sdk.Backend.Beta.Identity.Authorization;
using WoW.Two.Sdk.Backend.Beta.Identity.Claims;
using WoW.Two.Sdk.Backend.Beta.Identity.Cookies;
using WoW.Two.Sdk.Backend.Beta.Identity.OAuth.GitHub;

namespace Drydock.Api.Auth;

/// <summary>Wires single-admin GitHub sign-in on the SDK identity primitives — cookie session, claim normalization, login allowlist, and a default-deny API.</summary>
public static class AuthConfigurationExtensions
{
    /// <summary>The cookie session scheme issued after a successful sign-in.</summary>
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>The GitHub OAuth challenge scheme.</summary>
    public const string GitHubScheme = "GitHub";

    /// <summary>The path GitHub redirects back to after authorization.</summary>
    public const string CallbackPath = "/api/identity/callback";

    /// <summary>The authorization policy protected endpoints require.</summary>
    public const string AdminPolicy = "DrydockAdmin";

    /// <summary>Binds the identity settings and wires cookie auth, GitHub OAuth, claim normalization, the login allowlist, and default-deny authorization.</summary>
    /// <param name="builder">The web application builder.</param>
    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<GitHubOAuthSettings>().Bind(builder.Configuration.GetSection("Identity:GitHub"));
        builder.Services.AddOptions<AuthSettings>().Bind(builder.Configuration.GetSection("Identity"));

        var gitHub = builder.Configuration.GetSection("Identity:GitHub").Get<GitHubOAuthSettings>() ?? new GitHubOAuthSettings();
        var authSettings = builder.Configuration.GetSection("Identity").Get<AuthSettings>() ?? new AuthSettings();

        // Cookie holds the session; API mode returns 401/403 (not a 302) so the SPA renders its own sign-in.
        builder.Services.AddCookieAuthentication(o =>
        {
            o.Mode = AuthChallengeMode.Api;
            o.CookieName = ".drydock.auth";
            o.ExpireTimeSpan = TimeSpan.FromHours(8);
        });

        // GitHub is the login provider — registered only when configured, so the host boots with empty creds.
        if (gitHub.IsConfigured)
        {
            builder.Services.AddAuthentication().AddGitHubAuthentication(
                gitHub.ClientId,
                gitHub.ClientSecret,
                configure: o =>
                {
                    o.CallbackPath = CallbackPath;
                    o.SignInScheme = CookieScheme;
                },
                "user:email", "repo", "read:packages");
        }

        // Normalize provider claims to wt:* so the allowlist and dashboard read one shape regardless of provider.
        builder.Services.AddClaimNormalization();

        // Allowlist keyed on the normalized username; empty (default) = open (self-hosted — the runner is the user).
        builder.Services.AddPrincipalAllowlist(o =>
        {
            foreach (var login in authSettings.AllowedGitHubLogins)
                o.Allowed.Add(login);
        });

        // Every endpoint requires the signed-in, allowlisted admin by default; /health opts out via [AllowAnonymous].
        builder.Services.AddDefaultDenyAuthorization(CookieScheme, withAllowlist: true);

        return builder;
    }

    /// <summary>Adds authentication and authorization middleware to the pipeline (call before MapControllers).</summary>
    /// <param name="app">The web application.</param>
    public static WebApplication UseDrydockAuth(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

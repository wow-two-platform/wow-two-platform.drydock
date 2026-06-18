using System.Security.Claims;
using Drydock.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Drydock.Api.Auth;

/// <summary>Configures single-admin GitHub OAuth sign-in (cookie session + allowlist) for the Drydock host.</summary>
public static class AuthConfigurationExtensions
{
    /// <summary>The cookie scheme name — the session cookie issued after a successful GitHub sign-in.</summary>
    public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>The GitHub OAuth challenge scheme.</summary>
    public const string GitHubScheme = "GitHub";

    /// <summary>The path GitHub redirects back to after the user authorizes the OAuth app.</summary>
    public const string CallbackPath = "/api/identity/callback";

    /// <summary>The authorization policy every protected endpoint requires — authenticated AND on the allowlist.</summary>
    public const string AdminPolicy = "DrydockAdmin";

    /// <summary>
    /// Configures authentication and authorization: binds <see cref="GitHubOAuthSettings"/> + <see cref="AuthSettings"/>,
    /// registers cookie + GitHub OAuth schemes, enforces the single-admin allowlist on sign-in, and installs a
    /// fallback policy so the whole API requires the admin by default.
    /// </summary>
    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<GitHubOAuthSettings>()
            .Bind(builder.Configuration.GetSection("Identity:GitHub"));

        builder.Services
            .AddOptions<AuthSettings>()
            .Bind(builder.Configuration.GetSection("Identity"));

        var gitHub = builder.Configuration.GetSection("Identity:GitHub").Get<GitHubOAuthSettings>() ?? new GitHubOAuthSettings();

        // Cookie holds the session after sign-in; GitHub is the challenge/login provider.
        // Only point the default challenge at GitHub when it's actually registered — otherwise a
        // protected request would try to challenge a non-existent scheme and 500. With empty creds
        // the challenge falls back to the cookie scheme, whose OnRedirectToLogin returns a clean 401.
        var auth = builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieScheme;
                if (gitHub.IsConfigured)
                    options.DefaultChallengeScheme = GitHubScheme;
            })
            .AddCookie(CookieScheme, options =>
            {
                options.Cookie.Name = ".drydock.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;

                // Same-origin SPA: never 302 an API call to an HTML login page — return raw 401/403
                // and let the React app decide to show the sign-in screen.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        // Only register the GitHub provider when configured — keeps the host bootable with empty
        // default secrets (the build + /health work; sign-in surfaces a clear 503 until creds are set).
        if (gitHub.IsConfigured)
        {
            auth.AddGitHubAuthentication(
                gitHub.ClientId,
                gitHub.ClientSecret,
                options =>
                {
                    options.CallbackPath = CallbackPath;
                    options.SignInScheme = CookieScheme;

                    // GitHub maps `login`→ClaimTypes.Name, `id`→ClaimTypes.NameIdentifier, `name`→urn:github:name
                    // by default, but not the avatar — map it so the dashboard header can show it.
                    options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

                    // OPTIONAL ALLOWLIST LOCK — empty (default) = OPEN: any authenticated GitHub user passes
                    // (self-hosted, the runner is the user). If Auth:AllowedGitHubLogins is populated and the
                    // login isn't on it, fail the ticket: no cookie, bounced to access-denied (→ 403).
                    // The GitHub handler maps `login` → ClaimTypes.Name, `id` → ClaimTypes.NameIdentifier.
                    options.Events.OnCreatingTicket = context =>
                    {
                        var allow = context.HttpContext.RequestServices
                            .GetRequiredService<IOptions<AuthSettings>>().Value;
                        var login = context.Principal?.FindFirstValue(ClaimTypes.Name);

                        if (!allow.IsAllowed(login))
                            context.Fail($"GitHub login '{login}' is not on the Drydock admin allowlist.");

                        return Task.CompletedTask;
                    };
                },
                // Scopes: identity ("user:email") + "repo" so the signed-in user's token can read
                // their repositories — public AND private — for product repo-existence verification.
                // Widening "repo" requires re-consent: the admin must sign out and back in to mint a
                // token carrying the new scope. Add "read:packages" later when deploys pull from GHCR.
                "user:email", "repo");
        }

        // Fallback policy: every endpoint without an explicit [Authorize]/[AllowAnonymous] still requires the
        // signed-in admin. /health opts out via [AllowAnonymous]; the auth endpoints declare their own access.
        //
        // Both policies authenticate against the COOKIE scheme only — so an unauthenticated API call gets a
        // clean 401 (cookie OnRedirectToLogin) instead of a 302 to github.com. The same-origin SPA needs the
        // 401 to render its sign-in screen; interactive login goes through GET /api/identity/sign-in explicitly.
        var cookieOnly = new AuthorizationPolicyBuilder(CookieScheme)
            .RequireAuthenticatedUser()
            .Build();

        builder.Services
            .AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, cookieOnly)
            .SetFallbackPolicy(cookieOnly);

        return builder;
    }

    /// <summary>Adds authentication + authorization middleware to the pipeline (call before MapControllers).</summary>
    public static WebApplication UseDrydockAuth(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

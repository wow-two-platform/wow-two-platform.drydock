using System.Security.Claims;
using System.Text.Encodings.Web;
using Drydock.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Drydock.IntegrationTests.Harness;

/// <summary>
/// A test authentication handler that authenticates every request as a valid Drydock admin. Registered
/// only in the test host (via <see cref="TestAuthExtensions.UseTestAdminAuth"/>) so <c>[Authorize]</c> and
/// the fallback admin policy pass without the real GitHub-OAuth cookie flow.
/// </summary>
/// <remarks>
/// Drydock's production policies (<see cref="AuthConfigurationExtensions.AdminPolicy"/> + the fallback) are
/// built against the <see cref="AuthConfigurationExtensions.CookieScheme"/> and only require an authenticated
/// user. The test host re-points those policies at <see cref="SchemeName"/> and registers this handler, so an
/// authenticated principal here satisfies them. Anonymous clients simply omit the trigger header.
/// </remarks>
public sealed class TestAuthHandler(
    IOptionsMonitor<TestAuthSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthSchemeOptions>(options, logger, encoder)
{
    /// <summary>The test scheme name the host's admin policies are re-pointed at.</summary>
    public const string SchemeName = "Test";

    /// <summary>
    /// Request header that toggles authentication. Present (any value) → authenticate as admin; absent →
    /// no result, so the request is treated as anonymous and protected endpoints return 401.
    /// </summary>
    public const string AdminHeader = "X-Test-Admin";

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(AdminHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-admin-id"),
            new Claim(ClaimTypes.Name, Options.AdminLogin),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>Options for the <see cref="TestAuthHandler"/>.</summary>
public sealed class TestAuthSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>The GitHub login the test principal carries (only relevant if an allowlist is configured).</summary>
    public string AdminLogin { get; set; } = "test-admin";
}

/// <summary>Wires the test authentication scheme + re-points Drydock's admin policies at it.</summary>
public static class TestAuthExtensions
{
    /// <summary>
    /// Registers <see cref="TestAuthHandler"/> as the default scheme and rebinds both the named
    /// <see cref="AuthConfigurationExtensions.AdminPolicy"/> and the fallback policy onto it. Call from a
    /// <see cref="WebApiTestHost{TEntryPoint}.ConfigureServicesHook"/> so it overrides the real cookie/OAuth
    /// auth registered by the host.
    /// </summary>
    public static IServiceCollection UseTestAdminAuth(this IServiceCollection services)
    {
        // Make the test scheme the default so policies that don't name a scheme also resolve here.
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<TestAuthSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

        // The host's policies are bound to the cookie scheme; re-register them (same name → overwrite,
        // SetFallbackPolicy → last-write-wins) so they authenticate against the test scheme instead.
        var adminPolicy = new AuthorizationPolicyBuilder(TestAuthHandler.SchemeName)
            .RequireAuthenticatedUser()
            .Build();

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthConfigurationExtensions.AdminPolicy, adminPolicy)
            .SetFallbackPolicy(adminPolicy);

        return services;
    }
}

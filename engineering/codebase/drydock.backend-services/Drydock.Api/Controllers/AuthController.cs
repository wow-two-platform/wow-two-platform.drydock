using System.Security.Claims;
using Drydock.Api.Auth;
using Drydock.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Drydock.Api.Controllers;

/// <summary>Single-admin GitHub OAuth sign-in — challenge, current user, and logout.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IOptions<GitHubOAuthSettings> gitHub) : ControllerBase
{
    /// <summary>Begins GitHub OAuth: challenges the GitHub scheme and returns to <paramref name="returnUrl"/> after sign-in.</summary>
    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        // 503 until the OAuth app is configured — clearer than a generic 500 from an unregistered scheme.
        if (!gitHub.Value.IsConfigured)
            return Problem(
                detail: "GitHub sign-in is not configured. Set Identity:GitHub:ClientId and Identity:GitHub:ClientSecret (user-secrets).",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        // Local redirects only — never bounce the browser to an attacker-supplied absolute URL.
        var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        var properties = new AuthenticationProperties { RedirectUri = target };
        return Challenge(properties, AuthConfigurationExtensions.GitHubScheme);
    }

    /// <summary>Returns the signed-in admin (<c>login</c>, <c>name</c>, <c>avatar</c>) or 401 when not authenticated.</summary>
    [AllowAnonymous]
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        return Ok(new CurrentUser(
            Login: User.FindFirstValue(ClaimTypes.Name) ?? "",
            Name: User.FindFirstValue("urn:github:name") ?? User.FindFirstValue(ClaimTypes.Name) ?? "",
            Avatar: User.FindFirstValue("urn:github:avatar")));
    }

    /// <summary>Signs the admin out by clearing the session cookie.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>The current signed-in admin, as surfaced to the dashboard.</summary>
    /// <param name="Login">GitHub login handle.</param>
    /// <param name="Name">Display name (falls back to the login).</param>
    /// <param name="Avatar">Avatar URL, if GitHub provided one.</param>
    public sealed record CurrentUser(string Login, string Name, string? Avatar);
}

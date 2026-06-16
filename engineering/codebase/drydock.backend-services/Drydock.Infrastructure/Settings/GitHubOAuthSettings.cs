namespace Drydock.Infrastructure.Settings;

/// <summary>Configuration for the GitHub OAuth application used to sign the single admin in.</summary>
/// <example>Identity:GitHub</example>
public sealed record GitHubOAuthSettings
{
    /// <summary>Gets the GitHub OAuth app client id. Empty until configured via user-secrets.</summary>
    /// <example>Iv1.abc123def456</example>
    public string ClientId { get; init; } = "";

    /// <summary>Gets the GitHub OAuth app client secret. Set via user-secrets — never commit it.</summary>
    /// <example>0123456789abcdef0123456789abcdef01234567</example>
    public string ClientSecret { get; init; } = "";

    /// <summary>Gets a value indicating whether GitHub OAuth is configured (both id and secret present).</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

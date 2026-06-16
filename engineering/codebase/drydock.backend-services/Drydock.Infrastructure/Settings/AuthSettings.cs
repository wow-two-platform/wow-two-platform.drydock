namespace Drydock.Infrastructure.Settings;

/// <summary>Optional allowlist that locks who may sign in. Empty (default) = <b>open</b>: any authenticated
/// GitHub user is admitted (self-hosted — the runner is the user). Populate only to restrict an exposed instance.</summary>
/// <example>Auth</example>
public sealed record AuthSettings
{
    /// <summary>Gets the GitHub logins permitted to sign in. Empty = open (anyone with a GitHub account); when set, only these pass.</summary>
    /// <example>["my-github-handle"]</example>
    public IReadOnlyList<string> AllowedGitHubLogins { get; init; } = [];

    /// <summary>Returns true when <paramref name="login"/> is authenticated and the allowlist is either empty (open) or contains it (case-insensitive).</summary>
    public bool IsAllowed(string? login) =>
        !string.IsNullOrWhiteSpace(login)
        && (AllowedGitHubLogins.Count == 0
            || AllowedGitHubLogins.Any(l => string.Equals(l, login, StringComparison.OrdinalIgnoreCase)));
}

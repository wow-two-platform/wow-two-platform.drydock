namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>Rotation thresholds and the rules that flag overdue secrets and product tokens.</summary>
public static class VaultHygieneRules
{
    /// <summary>Days after which an active secret value is due for rotation.</summary>
    public const int SecretRotationDays = 90;

    /// <summary>Days after which a product token is due for rotation.</summary>
    public const int TokenRotationDays = 180;

    /// <summary>Days before its expiry at which a token is flagged.</summary>
    public const int ExpiryWarningDays = 14;

    /// <summary>Evaluates a vault's namespaces at <paramref name="now"/>.</summary>
    /// <param name="vault">The code-owned vault id.</param>
    /// <param name="namespaces">Every namespace's metadata.</param>
    /// <param name="now">The evaluation instant.</param>
    public static VaultHygiene Evaluate(string vault, IReadOnlyList<NamespaceInventory> namespaces, DateTimeOffset now)
    {
        var secrets = namespaces.SelectMany(ns => ns.Secrets).ToList();
        var tokens = namespaces.SelectMany(ns => ns.Tokens.Where(token => !token.IsRevoked).Select(token => (ns.Slug, Token: token))).ToList();
        var overdueSecrets = secrets
            .Where(secret => IsActive(secret) && secret.UpdatedAtUtc is { } updated && now - updated >= TimeSpan.FromDays(SecretRotationDays))
            .OrderBy(secret => secret.UpdatedAtUtc)
            .Select(secret => new OverdueSecret(secret.Namespace, secret.Key, secret.UpdatedAtUtc!.Value, Days(now - secret.UpdatedAtUtc.Value)))
            .ToList();
        var overdueTokens = tokens
            .Select(entry => (entry.Slug, entry.Token, Reason: TokenReason(entry.Token, now)))
            .Where(entry => entry.Reason is not null)
            .OrderBy(entry => entry.Token.CreatedAtUtc)
            .Select(entry => new OverdueToken(entry.Slug, entry.Token.Id, entry.Token.Name, entry.Token.CreatedAtUtc,
                Days(now - entry.Token.CreatedAtUtc), entry.Token.ExpiresAtUtc, entry.Reason!))
            .ToList();
        return new VaultHygiene(vault, namespaces.Count, secrets.Count, secrets.Count(secret => !IsActive(secret)), tokens.Count,
            overdueSecrets, overdueTokens, SecretRotationDays, TokenRotationDays);
    }

    private static bool IsActive(SecretFacts secret) => string.Equals(secret.State, "active", StringComparison.OrdinalIgnoreCase);

    private static string? TokenReason(TokenFacts token, DateTimeOffset now) =>
        token.ExpiresAtUtc is { } expiry && expiry <= now ? "expired"
        : token.ExpiresAtUtc is { } soon && soon - now <= TimeSpan.FromDays(ExpiryWarningDays) ? "expires soon"
        : now - token.CreatedAtUtc >= TimeSpan.FromDays(TokenRotationDays) ? "rotation due"
        : null;

    private static int Days(TimeSpan span) => (int)Math.Floor(span.TotalDays);
}

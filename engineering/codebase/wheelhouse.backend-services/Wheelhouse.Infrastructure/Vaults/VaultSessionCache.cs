using System.Collections.Concurrent;

namespace Wheelhouse.Infrastructure.Vaults;

/// <summary>Holds one administrator session per vault until shortly before it expires.</summary>
public sealed class VaultSessionCache(TimeProvider time)
{
    private static readonly TimeSpan Margin = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, (string Token, DateTimeOffset ExpiresAt)> _sessions = new();

    /// <summary>Gets a still-valid session token for a vault, or <see langword="null"/>.</summary>
    public string? Get(string vault) =>
        _sessions.TryGetValue(vault, out var session) && session.ExpiresAt - Margin > time.GetUtcNow() ? session.Token : null;

    /// <summary>Stores a vault's session token until its expiry.</summary>
    public void Set(string vault, string token, DateTimeOffset expiresAt) => _sessions[vault] = (token, expiresAt);

    /// <summary>Drops a vault's session after the vault rejected it.</summary>
    public void Forget(string vault) => _sessions.TryRemove(vault, out _);
}

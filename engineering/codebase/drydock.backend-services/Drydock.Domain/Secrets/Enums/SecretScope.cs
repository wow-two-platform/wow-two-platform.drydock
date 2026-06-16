namespace Drydock.Domain.Secrets.Enums;

/// <summary>What a stored secret is scoped to.</summary>
public enum SecretScope
{
    /// <summary>Applies platform-wide (e.g. the Cloudflare token).</summary>
    Global = 0,

    /// <summary>Bound to a single server (e.g. its SSH private key).</summary>
    Server,

    /// <summary>Bound to a product.</summary>
    Product,

    /// <summary>Bound to a product environment (e.g. injected env vars).</summary>
    Environment
}

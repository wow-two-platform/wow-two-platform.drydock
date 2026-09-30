namespace Wheelhouse.Tests.E2E.Support;

/// <summary>Response shape for a server (mirrors the host's <c>ServerDto</c> — enum read as its string name).</summary>
/// <param name="Id">Server id.</param>
/// <param name="Name">Friendly label.</param>
/// <param name="Host">IP or hostname.</param>
/// <param name="SshUser">Deploy user.</param>
/// <param name="Region">Hetzner region, when known.</param>
/// <param name="Status">Connectivity state (string-serialized enum).</param>
/// <param name="CreatedAtUtc">When the server was registered.</param>
public sealed record ServerResponse(
    Guid Id,
    string Name,
    string Host,
    string SshUser,
    string? Region,
    string Status,
    DateTimeOffset CreatedAtUtc);

/// <summary>Response shape for a catalog product (mirrors the host's <c>ProductDto</c> — enum read as its string name).</summary>
/// <param name="Slug">The product's identifier.</param>
/// <param name="Name">Display name.</param>
/// <param name="Description">What the product does.</param>
/// <param name="Lifecycle">Where it stands (string-serialized enum).</param>
/// <param name="Repository">Its source repository.</param>
/// <param name="IconUrl">The path of its icon.</param>
/// <param name="Environments">Its environments, dev to prod.</param>
public sealed record ProductResponse(
    string Slug,
    string Name,
    string Description,
    string Lifecycle,
    ProductRepositoryResponse Repository,
    string IconUrl,
    IReadOnlyList<ProductEnvironmentResponse> Environments);

/// <summary>Response shape for a catalog product's repository.</summary>
/// <param name="Name">The repository, as <c>owner/name</c>.</param>
/// <param name="Url">Its GitHub address.</param>
/// <param name="DefaultBranch">The branch releases come from.</param>
public sealed record ProductRepositoryResponse(string Name, string Url, string DefaultBranch);

/// <summary>Response shape for one environment of a catalog product.</summary>
/// <param name="Name">The environment.</param>
/// <param name="Sites">The sites it publishes.</param>
/// <param name="Secrets">Where its settings belong, when a vault serves it.</param>
public sealed record ProductEnvironmentResponse(
    string Name,
    IReadOnlyList<ProductSiteResponse> Sites,
    ProductSecretsResponse? Secrets);

/// <summary>Response shape for a site an environment publishes.</summary>
/// <param name="Name">The site's name.</param>
/// <param name="Url">Its address.</param>
/// <param name="Exposure"><c>public</c> or <c>private</c>.</param>
public sealed record ProductSiteResponse(string Name, string Url, string Exposure);

/// <summary>Response shape for where an environment's settings belong in a vault.</summary>
/// <param name="Vault">The vault.</param>
/// <param name="Namespace">The namespace.</param>
public sealed record ProductSecretsResponse(string Vault, string Namespace);

/// <summary>Response shape for an integration key (mirrors the host's <c>IntegrationKeyDto</c>).</summary>
/// <param name="Id">The key's identifier.</param>
/// <param name="Name">Its name.</param>
/// <param name="Prefix">The secret's first characters.</param>
/// <param name="Scopes">What it may reach.</param>
/// <param name="CreatedBy">Who created it.</param>
/// <param name="CreatedAt">When it was created.</param>
/// <param name="LastUsedAt">When it last authenticated a request.</param>
/// <param name="RevokedAt">When it was revoked.</param>
public sealed record IntegrationKeyResponse(
    Guid Id,
    string Name,
    string Prefix,
    IReadOnlyList<string> Scopes,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);

/// <summary>Response shape for a new integration key and its one-time secret.</summary>
/// <param name="Key">The key as kept.</param>
/// <param name="Secret">The secret, shown once.</param>
public sealed record IntegrationKeyWithSecretResponse(IntegrationKeyResponse Key, string Secret);

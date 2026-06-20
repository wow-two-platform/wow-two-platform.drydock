namespace Drydock.IntegrationTests.Support;

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

/// <summary>Response shape for a product (mirrors the host's <c>ProductDto</c> — enum read as its string name).</summary>
/// <param name="Id">Product id.</param>
/// <param name="Slug">URL-safe slug.</param>
/// <param name="Name">Display name.</param>
/// <param name="Repo">The GitHub <c>{owner}/{repo}</c>.</param>
/// <param name="Status">Lifecycle state (string-serialized enum).</param>
/// <param name="CreatedAtUtc">When the product was registered.</param>
public sealed record ProductResponse(
    Guid Id,
    string Slug,
    string Name,
    string Repo,
    string Status,
    DateTimeOffset CreatedAtUtc);

/// <summary>Response shape for a product's build/image status (mirrors the host's <c>ProductVersionDto</c> — state read as its string name).</summary>
/// <param name="State">Whether a ready, deployable image exists (string-serialized enum).</param>
/// <param name="LatestTag">The latest released version tag, when any.</param>
/// <param name="LatestAtUtc">When the latest release was published, when known.</param>
/// <param name="ReadyTag">The newest released tag with a ready image, when any.</param>
/// <param name="ReadyAtUtc">When the ready release was published, when known.</param>
/// <param name="Image">The ready image reference, when any.</param>
/// <param name="Detail">A short, human-readable reason for the state — shown on hover in the dashboard.</param>
public sealed record ProductVersionResponse(
    string State,
    string? LatestTag,
    DateTimeOffset? LatestAtUtc,
    string? ReadyTag,
    DateTimeOffset? ReadyAtUtc,
    string? Image,
    string? Detail);

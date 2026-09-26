namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>The product-token metadata hygiene reads.</summary>
/// <param name="Id">The token id.</param>
/// <param name="Name">The token name.</param>
/// <param name="CreatedAtUtc">When the token was minted.</param>
/// <param name="ExpiresAtUtc">The optional expiry.</param>
/// <param name="IsRevoked">Whether the token was revoked.</param>
public sealed record TokenFacts(Guid Id, string Name, DateTimeOffset CreatedAtUtc, DateTimeOffset? ExpiresAtUtc, bool IsRevoked);

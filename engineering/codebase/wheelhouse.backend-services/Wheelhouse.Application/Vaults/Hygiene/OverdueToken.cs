namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>A product token that expired, expires soon, or is older than the rotation threshold.</summary>
/// <param name="Namespace">The namespace slug.</param>
/// <param name="Id">The token id.</param>
/// <param name="Name">The token name.</param>
/// <param name="CreatedAtUtc">When the token was minted.</param>
/// <param name="AgeDays">Whole days since the token was minted.</param>
/// <param name="ExpiresAtUtc">The token's expiry, when it has one.</param>
/// <param name="Reason">Why it is flagged: <c>expired</c>, <c>expires soon</c> or <c>rotation due</c>.</param>
public sealed record OverdueToken(
    string Namespace, Guid Id, string Name, DateTimeOffset CreatedAtUtc, int AgeDays, DateTimeOffset? ExpiresAtUtc, string Reason);

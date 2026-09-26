namespace Wheelhouse.Application.Vaults.Changes;

/// <summary>Represents revoking a product token.</summary>
public sealed record TokenRevokeChange : VaultChange
{
    /// <summary>Gets the token id.</summary>
    public required Guid TokenId { get; init; }

    /// <inheritdoc />
    public override string Describe() => "revoke token " + TokenId + " for " + Namespace;
}

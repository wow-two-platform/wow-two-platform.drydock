namespace Wheelhouse.Application.Vaults.Changes;

/// <summary>Represents minting a namespace-scoped product token; the vault returns it once.</summary>
public sealed record TokenMintChange : VaultChange
{
    /// <summary>Gets the token's display name.</summary>
    public required string Name { get; init; }

    /// <inheritdoc />
    public override string Describe() => "mint token " + Name + " for " + Namespace;
}

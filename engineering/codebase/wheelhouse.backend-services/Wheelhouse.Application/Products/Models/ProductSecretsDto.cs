namespace Wheelhouse.Application.Products.Models;

/// <summary>Represents where a catalog product environment's settings belong in a vault.</summary>
public sealed record ProductSecretsDto
{
    /// <summary>Gets the vault's identifier.</summary>
    public required string Vault { get; init; }

    /// <summary>Gets the namespace, <c>{product}-{environment}</c>.</summary>
    public required string Namespace { get; init; }
}

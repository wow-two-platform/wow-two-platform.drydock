namespace Wheelhouse.Application.Products.Models;

/// <summary>Represents where an environment's settings belong in a vault.</summary>
public sealed record ProductSecretsModel
{
    /// <summary>Gets the vault on the environment's server.</summary>
    public required string VaultId { get; init; }

    /// <summary>Gets the namespace, <c>{product}-{environment}</c>.</summary>
    public required string Namespace { get; init; }
}

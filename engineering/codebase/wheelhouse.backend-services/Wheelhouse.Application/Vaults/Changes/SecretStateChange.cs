namespace Wheelhouse.Application.Vaults.Changes;

/// <summary>Represents disabling or re-enabling a secret.</summary>
public sealed record SecretStateChange : VaultChange
{
    /// <summary>Gets the secret key.</summary>
    public required string Key { get; init; }
    /// <summary>Gets whether the secret stops being served.</summary>
    public required bool Disabled { get; init; }

    /// <inheritdoc />
    public override string Describe() => (Disabled ? "disable secret " : "enable secret ") + Namespace + "/" + Key;
}

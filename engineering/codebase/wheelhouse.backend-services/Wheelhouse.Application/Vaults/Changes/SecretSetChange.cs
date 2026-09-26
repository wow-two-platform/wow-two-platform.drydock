namespace Wheelhouse.Application.Vaults.Changes;

/// <summary>Represents writing a new secret version; the value only travels to the vault.</summary>
public sealed record SecretSetChange : VaultChange
{
    /// <summary>Gets the secret key.</summary>
    public required string Key { get; init; }
    /// <summary>Gets the secret value.</summary>
    public required string Value { get; init; }
    /// <summary>Gets an optional description.</summary>
    public string? Description { get; init; }

    /// <inheritdoc />
    public override string Describe() => "set secret " + Namespace + "/" + Key;

    /// <summary>Returns the record without its value, so logs and exceptions cannot print it.</summary>
    public override string ToString() => nameof(SecretSetChange) + " { " + Namespace + "/" + Key + " }";
}

namespace Wheelhouse.Application.Vaults.Changes;

/// <summary>Represents creating a namespace.</summary>
public sealed record NamespaceCreateChange : VaultChange
{
    /// <summary>Gets the namespace's display name.</summary>
    public required string Name { get; init; }

    /// <inheritdoc />
    public override string Describe() => "create namespace " + Namespace;
}

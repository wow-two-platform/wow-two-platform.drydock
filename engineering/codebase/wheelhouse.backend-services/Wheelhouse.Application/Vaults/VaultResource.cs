namespace Wheelhouse.Application.Vaults;

/// <summary>Names the metadata a vault read returns; secret values are never readable.</summary>
public enum VaultResource
{
    /// <summary>The configured vaults with their sealed state.</summary>
    Vaults,
    /// <summary>The namespaces of one vault.</summary>
    Namespaces,
    /// <summary>The secret metadata of one namespace.</summary>
    Secrets,
    /// <summary>The product tokens of one namespace.</summary>
    Tokens
}

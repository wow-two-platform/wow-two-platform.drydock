namespace Wheelhouse.Domain.Integrations.Constants;

/// <summary>Holds the scopes an integration key can grant; a key reaches only what its scopes name.</summary>
public static class IntegrationScopeConstants
{
    /// <summary>Holds the scope that reads the product catalog: identities, lifecycle, environments, sites and vault
    /// namespaces.</summary>
    public const string CatalogRead = "catalog:read";

    /// <summary>Holds every scope a key can be created with.</summary>
    public static readonly IReadOnlyList<string> All = [CatalogRead];
}

namespace Wheelhouse.Api.Requests;

/// <summary>Holds the path-safe identifier patterns for vault routes and bodies.</summary>
public static class VaultRules
{
    /// <summary>A code-owned vault id.</summary>
    public const string Vault = "^[a-z][a-z0-9-]{0,47}$";
    /// <summary>A vault namespace slug.</summary>
    public const string Namespace = "^[a-z0-9][a-z0-9._-]{0,99}$";
    /// <summary>A secret key, including configuration paths such as <c>Billing:SecretKey</c>.</summary>
    public const string Key = "^[A-Za-z0-9][A-Za-z0-9._:-]{0,255}$";
}

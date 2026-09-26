namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>A vault's rotation hygiene across every namespace; metadata only, never values.</summary>
/// <param name="Vault">The code-owned vault id.</param>
/// <param name="Namespaces">How many namespaces the vault holds.</param>
/// <param name="Secrets">How many secrets the vault holds, disabled ones included.</param>
/// <param name="DisabledSecrets">How many secrets are disabled.</param>
/// <param name="Tokens">How many product tokens are not revoked.</param>
/// <param name="OverdueSecrets">Active secrets whose value is older than <paramref name="SecretRotationDays"/>, oldest first.</param>
/// <param name="OverdueTokens">Tokens that expired, expire soon, or are older than <paramref name="TokenRotationDays"/>, oldest first.</param>
/// <param name="SecretRotationDays">The secret rotation threshold in days.</param>
/// <param name="TokenRotationDays">The token rotation threshold in days.</param>
public sealed record VaultHygiene(
    string Vault,
    int Namespaces,
    int Secrets,
    int DisabledSecrets,
    int Tokens,
    IReadOnlyList<OverdueSecret> OverdueSecrets,
    IReadOnlyList<OverdueToken> OverdueTokens,
    int SecretRotationDays,
    int TokenRotationDays);

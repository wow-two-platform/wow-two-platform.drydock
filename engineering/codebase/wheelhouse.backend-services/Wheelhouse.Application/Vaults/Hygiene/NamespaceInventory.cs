namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>One namespace's secret and token metadata, as the vault reports it.</summary>
/// <param name="Slug">The namespace slug.</param>
/// <param name="Secrets">The namespace's secret metadata.</param>
/// <param name="Tokens">The namespace's product tokens.</param>
public sealed record NamespaceInventory(string Slug, IReadOnlyList<SecretFacts> Secrets, IReadOnlyList<TokenFacts> Tokens);

namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>The secret metadata hygiene reads; the vault never returns values.</summary>
/// <param name="Namespace">The namespace slug.</param>
/// <param name="Key">The secret key.</param>
/// <param name="State"><c>active</c> or <c>disabled</c>.</param>
/// <param name="UpdatedAtUtc">When the current value was written.</param>
public sealed record SecretFacts(string Namespace, string Key, string State, DateTimeOffset? UpdatedAtUtc);

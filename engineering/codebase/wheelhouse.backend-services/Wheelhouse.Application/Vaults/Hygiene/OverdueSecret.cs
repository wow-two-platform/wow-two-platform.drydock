namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>An active secret whose current value is older than the rotation threshold.</summary>
/// <param name="Namespace">The namespace slug.</param>
/// <param name="Key">The secret key.</param>
/// <param name="UpdatedAtUtc">When the current value was written.</param>
/// <param name="AgeDays">Whole days since the value was written.</param>
public sealed record OverdueSecret(string Namespace, string Key, DateTimeOffset UpdatedAtUtc, int AgeDays);

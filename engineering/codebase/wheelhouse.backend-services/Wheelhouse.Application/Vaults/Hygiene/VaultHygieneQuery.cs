using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Vaults.Hygiene;

/// <summary>Summarizes which of a vault's secrets and product tokens are due for rotation.</summary>
public sealed record VaultHygieneQuery(string Vault) : IQuery<AppResult<VaultHygiene>>;

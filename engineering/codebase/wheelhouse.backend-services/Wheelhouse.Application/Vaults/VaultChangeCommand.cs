using System.Text.Json;
using Wheelhouse.Application.Vaults.Changes;
using WoW.Two.Sdk.Backend.Beta.Mediator.Cqrs;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Wheelhouse.Application.Vaults;

/// <summary>Applies one administrative change to a vault on behalf of an operator.</summary>
public sealed record VaultChangeCommand(string Vault, VaultChange Change, string Actor) : ICommand<AppResult<JsonElement>>;

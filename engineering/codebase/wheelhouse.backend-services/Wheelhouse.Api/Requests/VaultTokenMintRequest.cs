using System.ComponentModel.DataAnnotations;

namespace Wheelhouse.Api.Requests;

/// <summary>Mints a namespace-scoped product token.</summary>
public sealed record VaultTokenMintRequest([Required, StringLength(200, MinimumLength = 1)] string Name);

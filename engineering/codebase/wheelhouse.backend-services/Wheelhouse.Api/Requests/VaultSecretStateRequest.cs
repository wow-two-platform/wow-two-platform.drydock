using System.ComponentModel.DataAnnotations;

namespace Wheelhouse.Api.Requests;

/// <summary>Disables or re-enables a secret.</summary>
public sealed record VaultSecretStateRequest([Required] bool? Disabled);

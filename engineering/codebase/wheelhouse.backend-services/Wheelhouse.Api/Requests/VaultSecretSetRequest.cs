using System.ComponentModel.DataAnnotations;

namespace Wheelhouse.Api.Requests;

/// <summary>Writes a new secret version; Wheelhouse forwards the value and never returns it.</summary>
public sealed record VaultSecretSetRequest(
    [Required, StringLength(65536)] string Value,
    [StringLength(1000)] string? Description)
{
    /// <summary>Returns the request without its value, so logs and exceptions cannot print it.</summary>
    public override string ToString() => nameof(VaultSecretSetRequest) + " { }";
}

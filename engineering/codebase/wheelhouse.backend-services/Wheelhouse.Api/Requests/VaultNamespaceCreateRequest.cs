using System.ComponentModel.DataAnnotations;

namespace Wheelhouse.Api.Requests;

/// <summary>Creates a vault namespace.</summary>
public sealed record VaultNamespaceCreateRequest(
    [Required, RegularExpression(VaultRules.Namespace)] string Slug,
    [Required, StringLength(200, MinimumLength = 1)] string Name);

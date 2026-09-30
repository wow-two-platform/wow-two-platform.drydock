namespace Wheelhouse.Application.Integrations.Models;

/// <summary>Represents a new integration key together with its secret, which is shown this once and never again.</summary>
public sealed record IntegrationKeyWithSecretDto
{
    /// <summary>Gets the key as it is kept.</summary>
    public required IntegrationKeyDto Key { get; init; }

    /// <summary>Gets the secret the calling program presents, as a Bearer token or in the <c>X-Api-Key</c> header.</summary>
    public required string Secret { get; init; }
}

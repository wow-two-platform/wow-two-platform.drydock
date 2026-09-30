namespace Wheelhouse.Api.Requests;

/// <summary>Represents the body of a request to create an integration key.</summary>
public sealed record CreateIntegrationKeyApiRequest
{
    /// <summary>Gets the name the key carries, such as the program that will present it.</summary>
    public required string Name { get; init; }

    /// <summary>Gets what the key may reach.</summary>
    public required IReadOnlyList<string> Scopes { get; init; }
}

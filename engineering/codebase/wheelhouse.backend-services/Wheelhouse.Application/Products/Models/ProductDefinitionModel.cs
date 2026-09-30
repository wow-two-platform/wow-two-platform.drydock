namespace Wheelhouse.Application.Products.Models;

/// <summary>Represents a product as the code-owned catalog and fleet define it.</summary>
public sealed record ProductDefinitionModel
{
    /// <summary>Gets the product's identifier everywhere: bundles, targets and the API.</summary>
    public required string Slug { get; init; }

    /// <summary>Gets the display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets one line on what the product does.</summary>
    public required string Description { get; init; }

    /// <summary>Gets the GitHub repository that defines the product, as <c>owner/name</c>.</summary>
    public required string Repository { get; init; }

    /// <summary>Gets the branch releases come from.</summary>
    public required string DefaultBranch { get; init; }

    /// <summary>Gets whether published releases and commit builds can be listed for the product.</summary>
    public required bool HasReleaseSource { get; init; }

    /// <summary>Gets one entry per fleet target of the product, in dev, test, prod order.</summary>
    public required IReadOnlyList<ProductEnvironmentModel> Environments { get; init; }
}

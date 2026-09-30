namespace Wheelhouse.Application.Products.Models;

/// <summary>Represents a catalog product's source repository.</summary>
public sealed record ProductRepositoryDto
{
    /// <summary>Gets the repository, as <c>owner/name</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the repository's GitHub address.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the branch releases come from.</summary>
    public required string DefaultBranch { get; init; }
}

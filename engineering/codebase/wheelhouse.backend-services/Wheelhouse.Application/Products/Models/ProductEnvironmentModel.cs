namespace Wheelhouse.Application.Products.Models;

/// <summary>Represents one environment of a catalog product, as a fleet target binds it.</summary>
public sealed record ProductEnvironmentModel
{
    /// <summary>Gets the environment: <c>dev</c>, <c>test</c> or <c>prod</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the fleet target that runs the environment.</summary>
    public required string TargetId { get; init; }

    /// <summary>Gets the server the target lives on.</summary>
    public required string ServerId { get; init; }

    /// <summary>Gets the sites the newest recorded rollout published.</summary>
    public required IReadOnlyList<ProductSiteModel> Sites { get; init; }

    /// <summary>Gets where the environment's settings belong in a vault, or <c>null</c> when its server has none.</summary>
    public ProductSecretsModel? Secrets { get; init; }
}

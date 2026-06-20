namespace Drydock.Application.Products.Models;

/// <summary>Represents a product's resolved build/image status — the latest released version and the newest one with a ready image.</summary>
/// <param name="State">Whether a ready, deployable image exists.</param>
/// <param name="LatestTag">The latest released version tag, when the repo has any releases.</param>
/// <param name="LatestAtUtc">When the latest release was published, when known.</param>
/// <param name="ReadyTag">The newest released tag whose image is published and ready, when one exists.</param>
/// <param name="ReadyAtUtc">When the ready release was published, when known.</param>
/// <param name="Image">The fully-qualified ready image reference (<c>ghcr.io/{owner}/{repo}:{tag}</c>), when one exists.</param>
/// <param name="Detail">A short, human-readable reason for the state — why no image is ready, or why the check couldn't complete and how to fix it. Surfaced on hover in the dashboard.</param>
public sealed record ProductVersionDto(
    ProductVersionState State,
    string? LatestTag,
    DateTimeOffset? LatestAtUtc,
    string? ReadyTag,
    DateTimeOffset? ReadyAtUtc,
    string? Image,
    string? Detail);

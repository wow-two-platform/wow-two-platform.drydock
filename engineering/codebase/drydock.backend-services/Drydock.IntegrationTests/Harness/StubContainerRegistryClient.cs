using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;

namespace Drydock.IntegrationTests.Harness;

/// <summary>
/// Test double for <see cref="IContainerRegistryClient"/> — short-circuits the GHCR manifest probe so the
/// Products vertical never reaches the real registry.
/// </summary>
/// <remarks>Defaults to <see cref="ImageCheck.Missing"/> for every tag. Add a tag to <see cref="ExistingTags"/> to
/// make that tag's image "published", or set <see cref="Override"/> to force one outcome for the unauthorized / failed paths.</remarks>
public sealed class StubContainerRegistryClient : IContainerRegistryClient
{
    /// <summary>The tags whose images report as published; any other tag reports <see cref="ImageCheck.Missing"/>.</summary>
    public HashSet<string> ExistingTags { get; } = new(StringComparer.Ordinal);

    /// <summary>When set, every probe returns this outcome regardless of <see cref="ExistingTags"/> (for unauthorized / failed paths).</summary>
    public ImageCheck? Override { get; set; }

    /// <inheritdoc />
    public Task<ImageCheck> ImageExistsAsync(string repo, string tag, CancellationToken ct)
    {
        if (Override is { } forced)
            return Task.FromResult(forced);

        var outcome = ExistingTags.Contains(tag) ? ImageCheck.Exists : ImageCheck.Missing;
        return Task.FromResult(outcome);
    }
}

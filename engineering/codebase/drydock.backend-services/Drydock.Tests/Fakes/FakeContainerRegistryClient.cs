using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;

namespace Drydock.Tests.Fakes;

/// <summary>
/// Configurable, in-memory <see cref="IContainerRegistryClient"/> for the unit tier — the GHCR manifest
/// probe returns a canned value per tag, with no registry call. Local to this project on purpose.
/// </summary>
/// <remarks>Defaults to <see cref="ImageCheck.Missing"/> for every tag. Add a tag to <see cref="ExistingTags"/>
/// to make that tag "published", or set <see cref="Override"/> to force one outcome (the Unauthorized / Failed paths).</remarks>
internal sealed class FakeContainerRegistryClient : IContainerRegistryClient
{
    /// <summary>Tags whose images report as published; any other tag reports <see cref="ImageCheck.Missing"/>.</summary>
    public HashSet<string> ExistingTags { get; } = new(StringComparer.Ordinal);

    /// <summary>When set, every probe returns this regardless of <see cref="ExistingTags"/>.</summary>
    public ImageCheck? Override { get; set; }

    public Task<ImageCheck> ImageExistsAsync(string repo, string tag, CancellationToken ct)
    {
        if (Override is { } forced)
            return Task.FromResult(forced);

        var outcome = ExistingTags.Contains(tag) ? ImageCheck.Exists : ImageCheck.Missing;
        return Task.FromResult(outcome);
    }
}

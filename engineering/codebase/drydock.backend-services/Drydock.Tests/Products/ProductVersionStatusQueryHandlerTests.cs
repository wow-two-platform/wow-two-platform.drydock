using AwesomeAssertions;
using Drydock.Application.Products.Models;
using Drydock.Application.Products.Queries.ProductVersionStatus;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Products.Enums;
using Drydock.Tests.Fakes;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Tests.Products;

using AppOutcome = AppResult<ProductVersionStatusResult.Success, ProductVersionStatusResult.Failure>;

/// <summary>
/// Unit tests for the version-resolution state machine in <see cref="ProductVersionStatusQueryHandler"/>.
/// Each of the eight <see cref="ProductVersionState"/> outcomes is driven purely by configuring the
/// GitHub / registry fakes — no DB, no HTTP. The handler's only persistence touch is a single
/// <c>FindAsync</c>, served by <see cref="FakeProductStore"/>.
/// </summary>
public sealed class ProductVersionStatusQueryHandlerTests
{
    private const string Repo = "wow-two-platform/drydock";
    private static readonly Guid ProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset V2At = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset V1At = new(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly FakeGitHubClient _gitHub = new();
    private readonly FakeContainerRegistryClient _registry = new();

    // ---- the eight states -------------------------------------------------

    [Fact]
    public async Task NoCi_when_publish_workflow_marker_is_absent()
    {
        _gitHub.Marker = FileCheck.Absent;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.NoCi);
        version.LatestTag.Should().BeNull();
        version.ReadyTag.Should().BeNull();
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task NeverBuilt_when_no_releases_and_no_latest_image()
    {
        // Marker present, lookup "found" but empty (collapses to None), and no :latest image.
        _gitHub.Releases = [];

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.NeverBuilt);
        version.LatestTag.Should().BeNull();
        version.ReadyTag.Should().BeNull();
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task NeverBuilt_when_release_exists_but_no_image_and_no_run()
    {
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At)];
        // registry: nothing published; publish run: never ran.
        _gitHub.PublishRun = BuildRunCheck.None;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.NeverBuilt);
        version.LatestTag.Should().Be("v2.0.0");
        version.LatestAtUtc.Should().Be(V2At);
        version.ReadyTag.Should().BeNull();
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task UnreleasedBuild_when_latest_image_published_without_a_release()
    {
        _gitHub.Releases = [];
        _registry.ExistingTags.Add("latest");

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.UnreleasedBuild);
        version.LatestTag.Should().BeNull();
        version.ReadyTag.Should().BeNull();
        version.Image.Should().Be("ghcr.io/wow-two-platform/drydock:latest");
    }

    [Fact]
    public async Task BuildPending_when_latest_release_image_run_is_running()
    {
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At)];
        _gitHub.PublishRun = BuildRunCheck.Running;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.BuildPending);
        version.LatestTag.Should().Be("v2.0.0");
        version.ReadyTag.Should().BeNull();
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task BuildPending_when_run_succeeded_but_image_not_yet_in_registry()
    {
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At)];
        _gitHub.PublishRun = BuildRunCheck.Succeeded;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.BuildPending);
        version.LatestTag.Should().Be("v2.0.0");
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task BuildFailed_when_latest_release_publish_run_failed()
    {
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At)];
        _gitHub.PublishRun = BuildRunCheck.Failed;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.BuildFailed);
        version.LatestTag.Should().Be("v2.0.0");
        version.ReadyTag.Should().BeNull();
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task Ready_when_latest_release_image_is_published()
    {
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At)];
        _registry.ExistingTags.Add("v2.0.0");

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.Ready);
        version.LatestTag.Should().Be("v2.0.0");
        version.ReadyTag.Should().Be("v2.0.0");
        version.ReadyAtUtc.Should().Be(V2At);
        version.Image.Should().Be("ghcr.io/wow-two-platform/drydock:v2.0.0");
    }

    [Fact]
    public async Task LatestNotReady_when_only_an_older_release_has_an_image()
    {
        // Newest-first: v2 is latest (no image), v1 is older (has image).
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At), new ReleaseInfo("v1.0.0", V1At)];
        _registry.ExistingTags.Add("v1.0.0");

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.LatestNotReady);
        version.LatestTag.Should().Be("v2.0.0");
        version.LatestAtUtc.Should().Be(V2At);
        version.ReadyTag.Should().Be("v1.0.0");
        version.ReadyAtUtc.Should().Be(V1At);
        version.Image.Should().Be("ghcr.io/wow-two-platform/drydock:v1.0.0");
    }

    [Fact]
    public async Task Unknown_when_github_marker_probe_is_unauthorized()
    {
        _gitHub.Marker = FileCheck.Unauthorized;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.Unknown);
        version.Detail.Should().Contain("Re-authorize");
    }

    [Fact]
    public async Task Unknown_when_github_marker_probe_fails()
    {
        _gitHub.Marker = FileCheck.Failed;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.Unknown);
        version.Detail.Should().Contain("Couldn't reach GitHub");
    }

    [Fact]
    public async Task Unknown_when_registry_probe_is_unauthorized()
    {
        _gitHub.Releases = [new ReleaseInfo("v2.0.0", V2At)];
        _registry.Override = ImageCheck.Unauthorized;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.Unknown);
        version.Detail.Should().Contain("read:packages");
    }

    [Fact]
    public async Task Unknown_when_release_lookup_is_unauthorized()
    {
        _gitHub.ReleaseOutcome = ReleaseLookup.Unauthorized;

        var version = await ResolveAsync();

        version.State.Should().Be(ProductVersionState.Unknown);
    }

    // ---- the persistence guard (the one non-version path) -----------------

    [Fact]
    public async Task NotFound_failure_when_product_does_not_exist()
    {
        var handler = new ProductVersionStatusQueryHandler(new FakeProductStore(), _gitHub, _registry);

        var result = await handler.HandleAsync(new ProductVersionStatusQuery(ProductId), CancellationToken.None);

        var failure = result.Should().BeOfType<AppOutcome.Failure>().Subject;
        failure.Error.Category.Should().Be(FailureCategory.NotFound);
    }

    // ---- helpers ----------------------------------------------------------

    /// <summary>Runs the handler against the configured fakes for an existing product and returns the resolved version DTO.</summary>
    private async Task<ProductVersionDto> ResolveAsync()
    {
        var product = new Product
        {
            Id = ProductId,
            Slug = "drydock",
            Name = "Drydock",
            Repo = Repo,
            Status = ProductStatus.Active,
            CreatedAtUtc = V1At
        };
        var handler = new ProductVersionStatusQueryHandler(new FakeProductStore(product), _gitHub, _registry);

        var result = await handler.HandleAsync(new ProductVersionStatusQuery(ProductId), CancellationToken.None);

        var success = result.Should().BeOfType<AppOutcome.Success>().Subject;
        return success.Data.Version;
    }
}

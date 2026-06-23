using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for the product version-status endpoint (<c>GET api/products/{id}/version</c>) — drives each state of the
/// build/image machine through the GitHub + GHCR stubs and asserts the resolved <c>ProductVersionDto</c>.
/// </summary>
[Collection(DrydockCollection.Name)]
public sealed class ProductVersionE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.drydock";

    [Fact]
    public async Task Version_Ready_WhenLatestReleaseHasImage()
    {
        var id = await RegisterProductAsync("ready-product");

        // Latest release v2.0.0 plus a published image for it → Ready.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v2.0.0"), Release("v1.0.0")];
        Fixture.Registry.ExistingTags.Add("v2.0.0");

        var version = await GetVersionAsync(id);

        version.State.Should().Be("Ready");
        version.LatestTag.Should().Be("v2.0.0");
        version.ReadyTag.Should().Be("v2.0.0");
        version.Image.Should().Be($"ghcr.io/{Repo}:v2.0.0");
    }

    [Fact]
    public async Task Version_LatestNotReady_WhenOnlyOlderReleaseHasImage()
    {
        var id = await RegisterProductAsync("lagging-product");

        // Latest v2.0.0 has no image; the older v1.0.0 does → LatestNotReady (ready lags the latest).
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v2.0.0"), Release("v1.0.0")];
        Fixture.Registry.ExistingTags.Add("v1.0.0");

        var version = await GetVersionAsync(id);

        version.State.Should().Be("LatestNotReady");
        version.LatestTag.Should().Be("v2.0.0");
        version.ReadyTag.Should().Be("v1.0.0");
        version.Image.Should().Be($"ghcr.io/{Repo}:v1.0.0");
    }

    [Fact]
    public async Task Version_NeverBuilt_WhenNoImageForAnyRelease()
    {
        var id = await RegisterProductAsync("never-built-product");

        // CI exists and there are releases, but no image is published for any of them → NeverBuilt.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        // Registry left empty → every tag is Missing.

        var version = await GetVersionAsync(id);

        version.State.Should().Be("NeverBuilt");
        version.ReadyTag.Should().BeNull();
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task Version_NeverBuilt_WhenNoReleasesYet()
    {
        var id = await RegisterProductAsync("no-release-product");

        // CI exists but the repo has published no releases → NeverBuilt.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [];

        var version = await GetVersionAsync(id);

        version.State.Should().Be("NeverBuilt");
        version.LatestTag.Should().BeNull();
        version.ReadyTag.Should().BeNull();
    }

    [Fact]
    public async Task Version_NoCi_WhenPublishWorkflowAbsent()
    {
        var id = await RegisterProductAsync("no-ci-product");

        // No publish workflow → nothing builds images for the repo → NoCi (terminal).
        Fixture.GitHub.Marker = FileCheck.Absent;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        Fixture.Registry.ExistingTags.Add("v1.0.0");

        var version = await GetVersionAsync(id);

        version.State.Should().Be("NoCi");
        version.LatestTag.Should().BeNull();
        version.ReadyTag.Should().BeNull();
    }

    [Fact]
    public async Task Version_Unknown_WhenRegistryUnauthorized()
    {
        var id = await RegisterProductAsync("unknown-product");

        // CI + a latest release, but the registry refuses the probe → Unknown (couldn't determine).
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        Fixture.Registry.Override = ImageCheck.Unauthorized;

        var version = await GetVersionAsync(id);

        version.State.Should().Be("Unknown");
        version.Detail.Should().Contain("read:packages");
    }

    [Fact]
    public async Task Version_UnreleasedBuild_WhenLatestImageButNoRelease()
    {
        var id = await RegisterProductAsync("unreleased-product");

        // CI exists, no releases, but a manual dispatch pushed :latest → UnreleasedBuild.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [];
        Fixture.Registry.ExistingTags.Add("latest");

        var version = await GetVersionAsync(id);

        version.State.Should().Be("UnreleasedBuild");
        version.LatestTag.Should().BeNull();
        version.Image.Should().Be($"ghcr.io/{Repo}:latest");
    }

    [Fact]
    public async Task Version_BuildPending_WhenLatestReleaseStillBuilding()
    {
        var id = await RegisterProductAsync("pending-product");

        // Latest release has no image and its publish run is in progress → BuildPending.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        Fixture.GitHub.PublishRun = BuildRunCheck.Running;

        var version = await GetVersionAsync(id);

        version.State.Should().Be("BuildPending");
        version.LatestTag.Should().Be("v1.0.0");
        version.Image.Should().BeNull();
    }

    [Fact]
    public async Task Version_BuildFailed_WhenLatestReleaseBuildFailed()
    {
        var id = await RegisterProductAsync("failed-product");

        // Latest release has no image and its publish run failed → BuildFailed.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        Fixture.GitHub.PublishRun = BuildRunCheck.Failed;

        var version = await GetVersionAsync(id);

        version.State.Should().Be("BuildFailed");
        version.LatestTag.Should().Be("v1.0.0");
    }

    [Fact]
    public async Task Version_NotFound_WhenProductMissing()
    {
        var response = await AdminClient.GetAsync($"api/products/{Guid.NewGuid()}/version");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static ReleaseInfo Release(string tag)
    {
        return new ReleaseInfo(tag, DateTimeOffset.UtcNow);
    }

    private async Task<Guid> RegisterProductAsync(string slug)
    {
        var body = new { slug, name = slug, repo = Repo };
        var response = await AdminClient.PostJsonAsync("api/products", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.ReadEnvelopeAsync<ProductResponse>();
        return product.Id;
    }

    private async Task<ProductVersionResponse> GetVersionAsync(Guid id)
    {
        var response = await AdminClient.GetAsync($"api/products/{id}/version");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.ReadEnvelopeAsync<ProductVersionResponse>();
    }
}

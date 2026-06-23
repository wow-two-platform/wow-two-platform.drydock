using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for version-resolution sub-paths the existing suite does not assert over HTTP — the transport-failure
/// (non-unauthorized) probe branches and the "build succeeded but image not yet in registry" pending branch. Each
/// drives the GitHub/GHCR stubs and asserts the resolved <c>ProductVersionDto</c> state + detail over JSON.
/// </summary>
[Collection(DrydockCollection.Name)]
public sealed class ProductVersionStatesE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.drydock";

    [Fact]
    public async Task Version_Unknown_WhenRegistryProbeFails()
    {
        var id = await RegisterProductAsync("registry-fail-product");

        // CI + a latest release, but the registry transport fails (not an auth denial) → Unknown with the reach-error detail.
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        Fixture.Registry.Override = ImageCheck.Failed;

        var version = await GetVersionAsync(id);

        version.State.Should().Be("Unknown");
        version.Detail.Should().Contain("container registry");
        version.Detail.Should().NotContain("read:packages"); // distinct from the unauthorized branch
        version.ReadyTag.Should().BeNull();
    }

    [Fact]
    public async Task Version_Unknown_WhenGitHubMarkerProbeFails()
    {
        var id = await RegisterProductAsync("github-fail-product");

        // The very first probe (publish-workflow marker) fails to reach GitHub → Unknown, terminal before releases.
        Fixture.GitHub.Marker = FileCheck.Failed;

        var version = await GetVersionAsync(id);

        version.State.Should().Be("Unknown");
        version.Detail.Should().Contain("GitHub");
        version.LatestTag.Should().BeNull();
    }

    [Fact]
    public async Task Version_BuildPending_WhenLatestReleaseBuildSucceededButImageMissing()
    {
        var id = await RegisterProductAsync("succeeded-not-pushed-product");

        // Latest release has no image, but its publish run succeeded → BuildPending (image not in the registry yet).
        Fixture.GitHub.Marker = FileCheck.Present;
        Fixture.GitHub.Releases = [Release("v1.0.0")];
        Fixture.GitHub.PublishRun = BuildRunCheck.Succeeded;
        // Registry left empty → the v1.0.0 image is Missing.

        var version = await GetVersionAsync(id);

        version.State.Should().Be("BuildPending");
        version.LatestTag.Should().Be("v1.0.0");
        version.Image.Should().BeNull();
        version.Detail.Should().Contain("registry"); // the succeeded-but-not-pushed wording
    }

    private static ReleaseInfo Release(string tag) => new(tag, DateTimeOffset.UtcNow);

    private async Task<Guid> RegisterProductAsync(string slug)
    {
        var response = await AdminClient.PostJsonAsync("api/products", new { slug, name = slug, repo = Repo });
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

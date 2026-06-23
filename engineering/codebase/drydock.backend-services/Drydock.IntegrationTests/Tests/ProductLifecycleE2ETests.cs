using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for the Products update + delete verticals — the full mutate-then-read round-trips that only the real host
/// exercises (PUT applies and persists; DELETE removes so a later GET 404s). The GitHub repo-existence probe is
/// stubbed to <c>Exists</c> by the fixture, so neither create nor the repo-change re-check hits the network.
/// </summary>
[Collection(DrydockCollection.Name)]
public sealed class ProductLifecycleE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.drydock";
    private const string OtherRepo = "wow-two-platform/wow-two-platform.secrets-vault";

    private static object CreateBody(string slug, string name, string repo = Repo) => new { slug, name, repo };

    private static object UpdateBody(string name, string repo, string status) => new { name, repo, status };

    [Fact]
    public async Task Put_Admin_UpdatesNameRepoAndStatus_Returns200WithUpdatedDto()
    {
        var id = await CreateProductAsync("upd-product", "Original Name");

        // Change every mutable field at once (slug is immutable and not in the PUT body).
        var response = await AdminClient.PutJsonAsync($"api/products/{id}", UpdateBody("Renamed Product", OtherRepo, "Active"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.ReadEnvelopeAsync<ProductResponse>();
        updated.Id.Should().Be(id);
        updated.Slug.Should().Be("upd-product"); // slug is immutable — survives the update
        updated.Name.Should().Be("Renamed Product");
        updated.Repo.Should().Be(OtherRepo);
        updated.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Put_Admin_PersistsChange_VisibleOnSubsequentGet()
    {
        var id = await CreateProductAsync("upd-persist", "Before");

        var put = await AdminClient.PutJsonAsync($"api/products/{id}", UpdateBody("After", Repo, "Paused"));
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        // Re-read through a fresh request — the change must be durable, not just echoed by the PUT.
        var get = await AdminClient.GetAsync($"api/products/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var reread = await get.ReadEnvelopeAsync<ProductResponse>();
        reread.Name.Should().Be("After");
        reread.Status.Should().Be("Paused");
    }

    [Fact]
    public async Task Put_MissingId_Returns404()
    {
        var response = await AdminClient.PutJsonAsync($"api/products/{Guid.NewGuid()}", UpdateBody("Ghost", Repo, "Active"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Admin_Returns204_ThenGetReturns404()
    {
        var id = await CreateProductAsync("del-product", "To Delete");

        var delete = await AdminClient.DeleteAsync($"api/products/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The row is gone — a follow-up read must 404.
        var get = await AdminClient.GetAsync($"api/products/{id}");
        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_MissingId_Returns404()
    {
        var response = await AdminClient.DeleteAsync($"api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateProductAsync(string slug, string name)
    {
        var response = await AdminClient.PostJsonAsync("api/products", CreateBody(slug, name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var product = await response.ReadEnvelopeAsync<ProductResponse>();
        return product.Id;
    }
}

using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for the Products vertical — create, the duplicate-slug conflict, and list. The GitHub repo-existence
/// probe is stubbed to <c>Exists</c> by the fixture, so creation never hits the network.
/// </summary>
[Collection(DrydockCollection.Name)]
public sealed class ProductsE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private static object ProductBody(string slug, string name, string repo = "wow-two-platform/wow-two-platform.drydock") => new
    {
        slug,
        name,
        repo,
    };

    [Fact]
    public async Task Post_Admin_ValidBody_Returns201()
    {
        var response = await AdminClient.PostJsonAsync("api/products", ProductBody("smart-qr", "Smart QR"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var product = await response.ReadEnvelopeAsync<ProductResponse>();
        product.Slug.Should().Be("smart-qr");
        product.Name.Should().Be("Smart QR");
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Post_DuplicateSlug_Returns409()
    {
        var first = await AdminClient.PostJsonAsync("api/products", ProductBody("dup-slug", "First"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await AdminClient.PostJsonAsync("api/products", ProductBody("dup-slug", "Second"));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_InvalidRepo_Returns400()
    {
        var response = await AdminClient.PostJsonAsync("api/products", ProductBody("bad-repo", "Bad Repo", "not-a-valid-repo"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Admin_Returns200()
    {
        var response = await AdminClient.GetAsync("api/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ReadEnvelopeAsync<IReadOnlyList<ProductResponse>>();
        products.Should().BeEmpty();
    }
}

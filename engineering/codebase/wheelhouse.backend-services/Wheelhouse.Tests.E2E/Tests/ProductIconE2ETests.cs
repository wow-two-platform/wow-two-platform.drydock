using System.Net;
using AwesomeAssertions;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Tests.E2E.Harness;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>E2E for product icons: the repository's own icon, or not found so the console shows a monogram.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class ProductIconE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    [Fact]
    public async Task GetIcon_ShouldReturn200AsADownload_WhenTheRepositoryCarriesAnIcon()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray();
        Fixture.ProductIcons.Icons["sulton-max/10x-venture-forever-pin"] =
            new ProductIconImage(svg, "image/svg+xml", "web/public/favicon.svg");

        var response = await AdminClient.GetAsync("api/products/foreverpin/icon");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(svg);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task GetIcon_ShouldReturn404_WhenTheRepositoryCarriesNone()
    {
        var response = await AdminClient.GetAsync("api/products/wheelhouse/icon");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIcon_ShouldReturn404_WhenTheCatalogLacksTheProduct()
    {
        var response = await AdminClient.GetAsync("api/products/ghost/icon");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIcon_ShouldReturn401_WhenTheCallerIsAnonymous()
    {
        var response = await AnonymousClient.GetAsync("api/products/foreverpin/icon");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

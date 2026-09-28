using System.Net;
using AwesomeAssertions;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Tests.E2E.Harness;
using Wheelhouse.Tests.E2E.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>E2E for product icons: the repository's own icon, or not found so the console shows a monogram.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class ProductIconE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    private async Task<Guid> RegisterAsync(string slug, string repo)
    {
        var response = await AdminClient.PostJsonAsync("api/products", new { slug, name = slug, repo });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.ReadEnvelopeAsync<ProductResponse>()).Id;
    }

    [Fact]
    public async Task Get_RepositoryWithIcon_ServesItAsADownload()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray();
        Fixture.ProductIcons.Icons["owner/iconic"] = new ProductIconImage(svg, "image/svg+xml", "web/public/favicon.svg");
        var id = await RegisterAsync("iconic", "owner/iconic");

        var response = await AdminClient.GetAsync($"api/products/{id}/icon");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(svg);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
    }

    [Fact]
    public async Task Get_RepositoryWithoutIcon_Returns404()
    {
        var id = await RegisterAsync("plain", "owner/plain");

        var response = await AdminClient.GetAsync($"api/products/{id}/icon");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Anonymous_Returns401()
    {
        var response = await Fixture.CreateAnonymousClient().GetAsync($"api/products/{Guid.NewGuid()}/icon");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

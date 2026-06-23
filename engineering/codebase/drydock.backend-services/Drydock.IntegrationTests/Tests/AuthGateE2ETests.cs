using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for the auth gate across the whole control-plane surface — the fallback admin policy must 401 every
/// anonymous request to a Products/Servers endpoint (reads and mutations alike), while the same call carrying the
/// admin header succeeds. Goes wide where the existing suite only proved one Servers GET 401.
/// </summary>
[Collection(DrydockCollection.Name)]
public sealed class AuthGateE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.drydock";

    private static object ProductBody => new { slug = "auth-probe", name = "Auth Probe", repo = Repo };

    private static object ServerBody => new { name = "auth-srv", host = "10.9.9.9", sshUser = "root", sshPort = 22, region = "hel1" };

    [Fact]
    public async Task Anonymous_GetProducts_Returns401()
    {
        var response = await AnonymousClient.GetAsync("api/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_PostProducts_Returns401()
    {
        var response = await AnonymousClient.PostJsonAsync("api/products", ProductBody);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_PutProducts_Returns401()
    {
        var response = await AnonymousClient.PutJsonAsync($"api/products/{Guid.NewGuid()}",
            new { name = "X", repo = Repo, status = "Active" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_DeleteProducts_Returns401()
    {
        var response = await AnonymousClient.DeleteAsync($"api/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_GetProductVersion_Returns401()
    {
        var response = await AnonymousClient.GetAsync($"api/products/{Guid.NewGuid()}/version");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Anonymous_PostServers_Returns401()
    {
        var response = await AnonymousClient.PostJsonAsync("api/servers", ServerBody);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_MutatingEndpoints_AreReachable()
    {
        // The same calls that 401 anonymously must pass for an authed admin — proves the gate, not a blanket block.
        var createProduct = await AdminClient.PostJsonAsync("api/products", ProductBody);
        createProduct.StatusCode.Should().Be(HttpStatusCode.Created);

        var createServer = await AdminClient.PostJsonAsync("api/servers", ServerBody);
        createServer.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

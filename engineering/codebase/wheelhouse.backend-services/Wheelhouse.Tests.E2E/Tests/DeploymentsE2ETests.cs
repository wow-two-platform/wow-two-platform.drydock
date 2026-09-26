using System.Net;
using System.Net.Http.Json;
using Wheelhouse.Tests.E2E.Harness;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>Verifies authorization and explicit selection at the deployment boundary.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class DeploymentsE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    [Theory]
    [InlineData("/api/deployments/targets")]
    [InlineData("/api/deployments/releases")]
    public async Task Inventory_RequiresAdmin(string path)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await AnonymousClient.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AdminClient.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Start_RequiresExplicitActionHeader()
    {
        var response = await AdminClient.PostAsJsonAsync("/api/deployments", new { target = "pilot", release = "v1" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Start_QueuesSelectedRelease()
    {
        var client = AdminClient;
        client.DefaultRequestHeaders.Add("X-Wheelhouse-Action", "deploy");
        var response = await client.PostAsJsonAsync("/api/deployments", new { target = "pilot", release = "v1" });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("pilot", Fixture.Deployments.LastTarget);
        Assert.Equal("v1", Fixture.Deployments.LastRelease);
    }

    [Fact]
    public async Task Start_RejectsPathsAndShellSyntax()
    {
        var client = AdminClient;
        client.DefaultRequestHeaders.Add("X-Wheelhouse-Action", "deploy");
        var response = await client.PostAsJsonAsync("/api/deployments", new { target = "../outside", release = "v1;id" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Start_RejectsAnonymousEvenWithActionHeader()
    {
        var client = AnonymousClient;
        client.DefaultRequestHeaders.Add("X-Wheelhouse-Action", "deploy");
        var response = await client.PostAsJsonAsync("/api/deployments", new { target = "pilot", release = "v1" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

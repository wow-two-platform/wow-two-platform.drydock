using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Wheelhouse.Tests.E2E.Harness;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>Verifies the read-only, code-owned fleet boundary.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class ServersE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    [Fact]
    public async Task Get_Anonymous_Returns401()
    {
        var response = await AnonymousClient.GetAsync("api/servers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Admin_ReturnsConfiguredProvider()
    {
        var response = await AdminClient.GetAsync("api/servers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var servers = await response.ReadEnvelopeAsync<JsonElement>();
        servers[0].GetProperty("id").GetString().Should().Be("pilot-host");
        servers[0].GetProperty("provider").GetString().Should().Be("Hetzner");
    }

    [Fact]
    public async Task Post_Admin_CannotRegisterHost()
    {
        var response = await AdminClient.PostJsonAsync("api/servers", new { name = "unreviewed", host = "10.0.0.1" });
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Delete_Admin_CannotRemoveHost()
    {
        var response = await AdminClient.DeleteAsync($"api/servers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}

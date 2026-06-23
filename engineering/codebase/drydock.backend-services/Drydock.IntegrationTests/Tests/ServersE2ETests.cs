using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>E2E for the Servers vertical — auth gate, list, create, and the duplicate-host conflict.</summary>
[Collection(DrydockCollection.Name)]
public sealed class ServersE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private static object ServerBody(string name, string host) => new
    {
        name,
        host,
        sshUser = "root",
        sshPort = 22,
        region = "hel1",
    };

    [Fact]
    public async Task Get_Anonymous_Returns401()
    {
        var response = await AnonymousClient.GetAsync("api/servers");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Admin_Returns200()
    {
        var response = await AdminClient.GetAsync("api/servers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var servers = await response.ReadEnvelopeAsync<IReadOnlyList<ServerResponse>>();
        servers.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_Admin_ValidBody_Returns201()
    {
        var response = await AdminClient.PostJsonAsync("api/servers", ServerBody("hel1-prod", "10.0.0.1"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var server = await response.ReadEnvelopeAsync<ServerResponse>();
        server.Name.Should().Be("hel1-prod");
        server.Host.Should().Be("10.0.0.1");
        server.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Post_DuplicateHost_Returns409()
    {
        var first = await AdminClient.PostJsonAsync("api/servers", ServerBody("first", "10.0.0.9"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await AdminClient.PostJsonAsync("api/servers", ServerBody("second", "10.0.0.9"));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

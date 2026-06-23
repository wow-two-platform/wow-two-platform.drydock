using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for list ordering — both <c>GET</c> collections return rows newest-first (<c>CreatedAtUtc</c> descending), the
/// order the dashboard relies on.
/// </summary>
/// <remarks>
/// The host runs on the harness <c>FakeTimeProvider</c> (<see cref="DrydockAppFixture.Host"/><c>.Clock</c>), so without
/// intervention every row would stamp the same instant and the sort would be a no-op. Each insert advances the fake
/// clock, giving rows distinct, deterministic timestamps — the order is then asserted exactly, with zero wall-clock
/// flakiness. (The store sorts on <c>CreatedAtUtc</c> alone, no secondary key, so same-instant ties are undefined — see
/// the note returned with this work.)
/// </remarks>
[Collection(DrydockCollection.Name)]
public sealed class ListOrderingE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.drydock";

    /// <summary>Per-insert clock advance so each row gets its own timestamp tick.</summary>
    private static readonly TimeSpan Tick = TimeSpan.FromMinutes(1);

    [Fact]
    public async Task GetProducts_ReturnsNewestFirst()
    {
        // Created oldest → newest; the response must come back newest → oldest.
        var creationOrder = new[] { "order-a", "order-b", "order-c", "order-d" };
        foreach (var slug in creationOrder)
        {
            var created = await AdminClient.PostJsonAsync("api/products", new { slug, name = slug, repo = Repo });
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            Fixture.Host.Clock.Advance(Tick);
        }

        var response = await AdminClient.GetAsync("api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ReadEnvelopeAsync<IReadOnlyList<ProductResponse>>();

        products.Should().HaveCount(creationOrder.Length);
        products.Select(p => p.CreatedAtUtc).Should().BeInDescendingOrder();
        products.Select(p => p.Slug).Should().Equal(creationOrder.Reverse());
    }

    [Fact]
    public async Task GetServers_ReturnsNewestFirst()
    {
        var creationOrder = new[] { "10.1.0.1", "10.1.0.2", "10.1.0.3", "10.1.0.4" };
        foreach (var host in creationOrder)
        {
            var created = await AdminClient.PostJsonAsync("api/servers",
                new { name = host, host, sshUser = "root", sshPort = 22, region = "hel1" });
            created.StatusCode.Should().Be(HttpStatusCode.Created);
            Fixture.Host.Clock.Advance(Tick);
        }

        var response = await AdminClient.GetAsync("api/servers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var servers = await response.ReadEnvelopeAsync<IReadOnlyList<ServerResponse>>();

        servers.Should().HaveCount(creationOrder.Length);
        servers.Select(s => s.CreatedAtUtc).Should().BeInDescendingOrder();
        servers.Select(s => s.Host).Should().Equal(creationOrder.Reverse());
    }
}

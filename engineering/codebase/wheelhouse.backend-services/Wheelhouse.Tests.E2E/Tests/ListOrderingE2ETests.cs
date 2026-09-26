using System.Net;
using AwesomeAssertions;
using Wheelhouse.Tests.E2E.Harness;
using Wheelhouse.Tests.E2E.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>
/// E2E for list ordering — the product <c>GET</c> collection return rows newest-first (<c>CreatedAtUtc</c> descending), the
/// order the dashboard relies on.
/// </summary>
/// <remarks>
/// The host runs on the harness <c>FakeTimeProvider</c> (<see cref="WheelhouseAppFixture.Host"/><c>.Clock</c>), so without
/// intervention every row would stamp the same instant and the sort would be a no-op. Each insert advances the fake
/// clock, giving rows distinct, deterministic timestamps — the order is then asserted exactly, with zero wall-clock
/// flakiness. (The store sorts on <c>CreatedAtUtc</c> alone, no secondary key, so same-instant ties are undefined — see
/// the note returned with this work.)
/// </remarks>
[Collection(WheelhouseCollection.Name)]
public sealed class ListOrderingE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.wheelhouse";

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

}

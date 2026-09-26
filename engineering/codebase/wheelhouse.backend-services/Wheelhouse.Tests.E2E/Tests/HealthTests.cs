using System.Net;
using AwesomeAssertions;
using Wheelhouse.Tests.E2E.Harness;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>The anonymous liveness endpoint is reachable without auth.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class HealthTests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    [Fact]
    public async Task Health_Anonymous_Returns200()
    {
        var response = await AnonymousClient.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

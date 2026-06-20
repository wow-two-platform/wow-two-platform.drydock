using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;

namespace Drydock.IntegrationTests.Tests;

/// <summary>The anonymous liveness endpoint is reachable without auth.</summary>
[Collection(DrydockCollection.Name)]
public sealed class HealthTests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    [Fact]
    public async Task Health_Anonymous_Returns200()
    {
        var response = await AnonymousClient.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

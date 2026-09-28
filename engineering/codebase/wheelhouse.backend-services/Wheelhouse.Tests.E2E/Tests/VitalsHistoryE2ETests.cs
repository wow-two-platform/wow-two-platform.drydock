using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wheelhouse.Application.Operations;
using Wheelhouse.Tests.E2E.Harness;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>Verifies that a sampling pass stores every target's readings and the history endpoint returns them.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class VitalsHistoryE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    [Fact]
    public async Task History_RequiresAdmin() =>
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await AnonymousClient.GetAsync("/api/deployments/vitals/history")).StatusCode);

    [Fact]
    public async Task SamplingPass_StoresEachTargetsReadingForTheHistory()
    {
        await using (var scope = Fixture.Host.Services.CreateAsyncScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<VitalsSampling>().SampleOnceAsync(CancellationToken.None));

        var samples = (await AdminClient.GetFromJsonAsync<JsonElement>("/api/deployments/vitals/history?hours=1"))
            .GetProperty("data").EnumerateArray().ToArray();
        var sample = Assert.Single(samples);
        Assert.Equal(("pilot", "pilot-host", true), (sample.GetProperty("targetId").GetString(),
            sample.GetProperty("serverId").GetString(), sample.GetProperty("readable").GetBoolean()));
        Assert.Empty((await AdminClient.GetFromJsonAsync<JsonElement>("/api/deployments/vitals/history?target=other"))
            .GetProperty("data").EnumerateArray());
    }

    [Theory]
    [InlineData("?hours=0")]
    [InlineData("?hours=721")]
    [InlineData("?target=Not_A_Slug")]
    public async Task History_RejectsAnUnboundedWindowOrAnInvalidTarget(string query) =>
        Assert.Equal(HttpStatusCode.BadRequest,
            (await AdminClient.GetAsync("/api/deployments/vitals/history" + query)).StatusCode);
}

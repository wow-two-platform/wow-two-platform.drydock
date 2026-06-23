using System.Net;
using AwesomeAssertions;
using Drydock.IntegrationTests.Harness;
using Drydock.IntegrationTests.Support;
using WoW.Two.Sdk.Backend.Beta.Testing.Web;

namespace Drydock.IntegrationTests.Tests;

/// <summary>
/// E2E for invalid input over HTTP — a rejected command must come back as an RFC 7807 <c>ProblemDetails</c>
/// (<c>title</c>/<c>status</c>, content negotiated by the host), NOT the success <c>ApiResponse&lt;T&gt;</c> envelope.
/// Asserting the failure shape end-to-end is something only the real host pipeline can prove.
/// </summary>
[Collection(DrydockCollection.Name)]
public sealed class ValidationProblemDetailsE2ETests(DrydockAppFixture fixture) : DrydockE2EBase(fixture)
{
    private const string Repo = "wow-two-platform/wow-two-platform.drydock";

    [Fact]
    public async Task Post_Product_InvalidRepo_Returns400ProblemDetails()
    {
        var response = await AdminClient.PostJsonAsync("api/products", new { slug = "bad-repo", name = "Bad Repo", repo = "not-a-valid-repo" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_Product_EmptySlug_Returns400ProblemDetails()
    {
        var response = await AdminClient.PostJsonAsync("api/products", new { slug = "", name = "No Slug", repo = Repo });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
    }

    [Fact]
    public async Task Post_Server_EmptyHost_Returns400ProblemDetails()
    {
        var response = await AdminClient.PostJsonAsync("api/servers", new { name = "no-host", host = "", sshUser = "root", sshPort = 22, region = "hel1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_Server_MalformedHost_Returns400ProblemDetails()
    {
        // Non-empty but not a valid host shape (URL scheme) — previously slipped through, now rejected.
        var response = await AdminClient.PostJsonAsync("api/servers", new { name = "bad-host", host = "http://10.0.0.1", sshUser = "root", sshPort = 22, region = "hel1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_Server_OutOfRangePort_Returns400ProblemDetails()
    {
        // Out-of-range port — previously silently coerced to 22, now rejected.
        var response = await AdminClient.PostJsonAsync("api/servers", new { name = "bad-port", host = "10.0.0.1", sshUser = "root", sshPort = 70000, region = "hel1" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
        problem.Title.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Put_Product_InvalidRepo_Returns400ProblemDetails()
    {
        var create = await AdminClient.PostJsonAsync("api/products", new { slug = "put-bad-repo", name = "OK", repo = Repo });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = (await create.ReadEnvelopeAsync<ProductResponse>()).Id;

        var response = await AdminClient.PutJsonAsync($"api/products/{id}", new { name = "OK", repo = "https://github.com/owner/repo", status = "Active" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Status.Should().Be(400);
    }
}

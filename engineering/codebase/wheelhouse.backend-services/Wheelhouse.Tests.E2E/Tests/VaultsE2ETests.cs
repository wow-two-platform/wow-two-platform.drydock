using System.Net;
using System.Net.Http.Json;
using Wheelhouse.Application.Vaults.Changes;
using Wheelhouse.Tests.E2E.Harness;

namespace Wheelhouse.Tests.E2E.Tests;

/// <summary>Verifies authorization, explicit actions, identifier rules and value handling at the vault boundary.</summary>
[Collection(WheelhouseCollection.Name)]
public sealed class VaultsE2ETests(WheelhouseAppFixture fixture) : WheelhouseE2EBase(fixture)
{
    private HttpClient ActionClient()
    {
        var client = AdminClient;
        client.DefaultRequestHeaders.Add("X-Wheelhouse-Action", "vault");
        return client;
    }

    [Theory]
    [InlineData("/api/vaults")]
    [InlineData("/api/vaults/pilot-vault/namespaces")]
    [InlineData("/api/vaults/pilot-vault/secrets?ns=billing")]
    [InlineData("/api/vaults/pilot-vault/namespaces/billing/tokens")]
    [InlineData("/api/vaults/pilot-vault/hygiene")]
    public async Task Reads_RequireAdmin(string path)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await AnonymousClient.GetAsync(path)).StatusCode);
        var response = await AdminClient.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore, "Vault responses must never be cached.");
    }

    [Fact]
    public async Task Hygiene_FlagsOverdueSecretsAndTokensWithoutValues()
    {
        var response = await AdminClient.GetAsync("/api/vaults/pilot-vault/hygiene");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        Assert.Equal(("pilot-vault", 1, 1), (data.GetProperty("vault").GetString(), data.GetProperty("secrets").GetInt32(),
            data.GetProperty("tokens").GetInt32()));
        Assert.Equal("DATABASE_URL", data.GetProperty("overdueSecrets")[0].GetProperty("key").GetString());
        Assert.Equal("rotation due", data.GetProperty("overdueTokens")[0].GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Writes_RequireTheExplicitAction()
    {
        var response = await AdminClient.PutAsJsonAsync("/api/vaults/pilot-vault/secrets/billing/API_KEY", new { value = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetSecret_ForwardsTheValueAndNeverEchoesIt()
    {
        var response = await ActionClient().PutAsJsonAsync(
            "/api/vaults/pilot-vault/secrets/billing/Billing%3ASecretKey", new { value = "sk_live_DO_NOT_ECHO", description = "Stripe" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("DO_NOT_ECHO", await response.Content.ReadAsStringAsync());
        var change = Assert.IsType<SecretSetChange>(Fixture.Vaults.LastChange);
        Assert.Equal(("billing", "Billing:SecretKey", "sk_live_DO_NOT_ECHO"), (change.Namespace, change.Key, change.Value));
        Assert.DoesNotContain("DO_NOT_ECHO", change.ToString());
    }

    [Fact]
    public async Task SetSecret_RequiresAValue()
    {
        var response = await ActionClient().PutAsJsonAsync("/api/vaults/pilot-vault/secrets/billing/API_KEY", new { value = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/vaults/Pilot-Vault/secrets/billing/API_KEY")]
    [InlineData("/api/vaults/pilot-vault/secrets/Billing%20Space/API_KEY")]
    [InlineData("/api/vaults/pilot-vault/secrets/billing/..%2Fescape")]
    public async Task SetSecret_RejectsUnsafeIdentifiers(string path)
    {
        var response = await ActionClient().PutAsJsonAsync(path, new { value = "x" });
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound, response.StatusCode.ToString());
    }

    [Fact]
    public async Task MintToken_ReturnsTheTokenOnceWithoutCaching()
    {
        var response = await ActionClient().PostAsJsonAsync("/api/vaults/pilot-vault/namespaces/billing/tokens", new { name = "management" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Contains("vt_shown_once", await response.Content.ReadAsStringAsync());
        Assert.IsType<TokenMintChange>(Fixture.Vaults.LastChange);
    }

    [Fact]
    public async Task RevokeToken_ForwardsTheTokenId()
    {
        var token = Guid.NewGuid();
        var response = await ActionClient().PostAsync($"/api/vaults/pilot-vault/namespaces/billing/tokens/{token}/revoke", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(token, Assert.IsType<TokenRevokeChange>(Fixture.Vaults.LastChange).TokenId);
    }

    [Fact]
    public async Task SecretState_RequiresTheFlag()
    {
        var response = await ActionClient().PostAsJsonAsync("/api/vaults/pilot-vault/secrets/billing/API_KEY/state", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

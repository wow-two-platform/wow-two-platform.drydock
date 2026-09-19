using System.Text.Json;
using Drydock.Application.Abstractions;
using WoW.Two.Sdk.Backend.Beta.Mediator.Result;

namespace Drydock.Tests.E2E.Harness;

/// <summary>Records deployment requests without contacting a real server.</summary>
public sealed class StubDeploymentGateway : IDeploymentGateway
{
    public string? LastTarget { get; private set; }
    public string? LastRelease { get; private set; }

    public Task<AppResult<JsonElement>> ReadAsync(string resource, string? id, CancellationToken ct) =>
        Task.FromResult(AppResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(
            resource == "servers" ? new object[] { new { id = "pilot-host", name = "Pilot", provider = "Hetzner", host = "vps.example.net", region = "hel1", sshUser = "deploy" } }
                : new object[] { new { id = "pilot", product = "foreverpin", environment = "staging" } })));

    public Task<AppResult<JsonElement>> StartAsync(string target, string release, string actor, CancellationToken ct)
    {
        LastTarget = target;
        LastRelease = release;
        return Task.FromResult(AppResult<JsonElement>.Ok(JsonSerializer.SerializeToElement(
            new { id = Guid.NewGuid(), status = "queued" })));
    }
}

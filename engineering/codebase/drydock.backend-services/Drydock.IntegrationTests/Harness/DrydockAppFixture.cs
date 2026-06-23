using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;
using WoW.Two.Sdk.Backend.Beta.Testing;
using WoW.Two.Sdk.Backend.Beta.Testing.Containers;

namespace Drydock.IntegrationTests.Harness;

/// <summary>
/// Owns the single in-process Drydock.Api host the whole E2E run drives, backed by an ephemeral Postgres
/// container. The container + between-test reset are owned by the SDK <see cref="PostgresFixture"/> (Testcontainers
/// + Respawn); this fixture stays the only Drydock-specific piece — it knows the connection-string key, the
/// test-auth + GitHub/GHCR stub wiring, and composes the host on top of the SDK <see cref="WebApiTestHost{TEntryPoint}"/>.
/// </summary>
/// <remarks>
/// Lifecycle: start the Postgres fixture → build the host (its <c>InitializeAsync()</c> runs the bespoke SQL migrator,
/// so the schema exists) → snapshot the post-migration schema once via <see cref="PostgresFixture.InitializeRespawnerAsync"/>.
/// <see cref="ResetAsync"/> then truncates data (Respawn ignores <c>migration_history</c>, so migrations never re-run).
/// </remarks>
public sealed class DrydockAppFixture : IAsyncLifetime
{
    private readonly PostgresFixture _postgres = new(new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build());

    private WebApiTestHost<Program>? _host;
    private bool _respawnerReady;

    /// <summary>The booted Api host.</summary>
    public WebApiTestHost<Program> Host =>
        _host ?? throw new InvalidOperationException("Fixture not initialized — Host is null.");

    /// <summary>The shared GitHub stub — flip its <see cref="StubGitHubClient.Result"/> to drive failure paths.</summary>
    public StubGitHubClient GitHub { get; } = new();

    /// <summary>The shared GHCR stub — add tags to its <see cref="StubContainerRegistryClient.ExistingTags"/> to mark images published.</summary>
    public StubContainerRegistryClient Registry { get; } = new();

    /// <summary>A fresh anonymous client (no admin header) — protected endpoints return 401.</summary>
    public HttpClient CreateAnonymousClient() => Host.CreateClient();

    /// <summary>A fresh client authenticated as a valid admin (carries the test-admin header).</summary>
    public HttpClient CreateAdminClient()
    {
        var client = Host.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AdminHeader, "1");
        return client;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // AddPostgresPersistence resolves the connection string eagerly at service registration, where the host hook's
        // config layer isn't applied yet — so use the DB_CONNECTION env seam (it wins over config and is visible at
        // registration time) to point the SDK persistence bundle + bespoke migrator at the container DB. The in-memory
        // config below (the app's own ConnectionStrings:Drydock key) is the belt-and-suspenders for any later config read.
        Environment.SetEnvironmentVariable("DB_CONNECTION", _postgres.ConnectionString);

        _host = new WebApiTestHost<Program>
        {
            // The SDK host has no connection-string knob — inject it the way the app reads it (ConnectionStrings:Drydock).
            ConfigureHostHook = builder => builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Drydock"] = _postgres.ConnectionString,
                })),
            ConfigureServicesHook = services =>
            {
                services.UseTestAdminAuth();

                // Replace the real (network + OAuth-token) GitHub client with the shared stub.
                services.RemoveAll<IGitHubClient>();
                services.AddSingleton<IGitHubClient>(GitHub);

                // Replace the real GHCR client with the shared stub (no registry network calls).
                services.RemoveAll<IContainerRegistryClient>();
                services.AddSingleton<IContainerRegistryClient>(Registry);
            },
        };

        // Force the host to build and run its startup migration (the bespoke migrator creates the schema).
        _ = Host.Services;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.DisposeAsync();

        await _postgres.DisposeAsync();

        // Don't leak the container connection string into other test processes.
        Environment.SetEnvironmentVariable("DB_CONNECTION", null);
    }

    /// <summary>Truncates every data table between tests via Respawn (the migration history is preserved), and resets the stubs.</summary>
    public async Task ResetAsync()
    {
        // Snapshot the schema once, after the host has migrated — the respawner reflects the real post-migration shape.
        if (!_respawnerReady)
        {
            await _postgres.InitializeRespawnerAsync();
            _respawnerReady = true;
        }

        await _postgres.ResetAsync();
        ResetStubs();
    }

    /// <summary>Restores the shared stubs to their happy-path defaults so each test starts from a known state.</summary>
    private void ResetStubs()
    {
        GitHub.Result = RepoCheck.Exists;
        GitHub.Marker = FileCheck.Present;
        GitHub.Releases = [];
        GitHub.ReleaseOutcome = ReleaseLookup.Found;
        GitHub.PublishRun = BuildRunCheck.None;

        Registry.ExistingTags.Clear();
        Registry.Override = null;
    }
}

/// <summary>xUnit collection sharing one <see cref="DrydockAppFixture"/> across the whole E2E run.</summary>
[CollectionDefinition(DrydockCollection.Name)]
public sealed class DrydockCollection : ICollectionFixture<DrydockAppFixture>
{
    /// <summary>Collection name — every E2E class joins this so they share the host and run serially.</summary>
    public const string Name = "drydock-e2e";
}

/// <summary>
/// Convenience base for E2E tests — wires the shared fixture, resets the DB before each test, and exposes
/// fresh clients. Concrete test classes carry <c>[Collection(DrydockCollection.Name)]</c>.
/// </summary>
public abstract class DrydockE2EBase(DrydockAppFixture fixture) : IAsyncLifetime
{
    /// <summary>The shared app fixture (host + Postgres DB).</summary>
    protected DrydockAppFixture Fixture { get; } = fixture;

    /// <summary>An anonymous client (no admin header).</summary>
    protected HttpClient AnonymousClient => Fixture.CreateAnonymousClient();

    /// <summary>A client authenticated as a valid admin.</summary>
    protected HttpClient AdminClient => Fixture.CreateAdminClient();

    /// <inheritdoc />
    public async Task InitializeAsync() => await Fixture.ResetAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}

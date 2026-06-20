using Drydock.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using WoW.Two.Sdk.Backend.Beta.Data.Dapper;
using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke;

namespace Drydock.IntegrationTests.Harness;

/// <summary>
/// Owns the single in-process Drydock.Api host the whole E2E run drives, backed by an ephemeral Postgres
/// container (Testcontainers). The host migrates on boot (<c>app.InitializeAsync()</c> → the bespoke SQL
/// migrator), so the schema exists by the time the first client is created. <see cref="ResetAsync"/> drops and
/// re-migrates the DB between tests for isolation.
/// </summary>
/// <remarks>
/// This is the only Drydock-specific piece of the harness — it knows the connection-string key, the bespoke
/// migrator, and the test-auth + GitHub-stub wiring. The generic host wrapper
/// (<see cref="WebApiTestHost{TEntryPoint}"/>) stays extractable to the SDK.
/// </remarks>
public sealed class DrydockAppFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApiTestHost<Program>? _host;

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

        _host = new WebApiTestHost<Program>
        {
            ConnectionString = _postgres.GetConnectionString(),
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
    }

    /// <summary>Drops and re-creates the schema via the bespoke migrator so each test starts from an empty database.</summary>
    public async Task ResetAsync()
    {
        using var scope = Host.Services.CreateScope();

        // Wipe the schema (drops every table incl. migration_history), then let the migrator re-apply the baseline.
        var connections = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        await using (var connection = await connections.CreateOpenAsync())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "drop schema public cascade; create schema public;";
            await command.ExecuteNonQueryAsync();
        }

        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunnerService>();
        await runner.ApplyPendingAsync("test-reset");

        ResetStubs();
    }

    /// <summary>Restores the shared stubs to their happy-path defaults so each test starts from a known state.</summary>
    private void ResetStubs()
    {
        GitHub.Result = RepoCheck.Exists;
        GitHub.Marker = MarkerCheck.Present;
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

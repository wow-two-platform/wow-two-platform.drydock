using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke;

namespace Drydock.Api.Configurations;

/// <summary>Startup initialization — applies pending SQL migrations (the bespoke migrator creates the schema on first run).</summary>
public static class AppInitialization
{
    /// <summary>Ensures the database schema exists before the host starts serving.</summary>
    public static async Task InitializeAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        // CREATE DATABASE can't run inside the target DB — the dialect connects to the maintenance DB first.
        // Read the raw string from configuration: NpgsqlDataSource.ConnectionString redacts the password.
        var connectionString = DrydockDatabase.ConnectionString(services.GetRequiredService<IConfiguration>());

        var dialect = services.GetRequiredService<IMigrationDialect>();
        await dialect.EnsureDatabaseExistsAsync(connectionString);

        var runner = services.GetRequiredService<IMigrationRunnerService>();
        await runner.ApplyPendingAsync("startup");
    }
}

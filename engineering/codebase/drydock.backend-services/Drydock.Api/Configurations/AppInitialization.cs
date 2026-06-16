using Drydock.Persistence;

namespace Drydock.Api.Configurations;

/// <summary>Startup initialization — applies migrations / creates the SQLite schema on first run.</summary>
public static class AppInitialization
{
    /// <summary>Ensures the database schema exists before the host starts serving.</summary>
    public static async Task InitializeAsync(this WebApplication app)
    {
        await app.Services.InitializeDatabaseAsync();
    }
}

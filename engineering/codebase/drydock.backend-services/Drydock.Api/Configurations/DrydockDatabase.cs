namespace Drydock.Api.Configurations;

/// <summary>Resolves the Drydock database connection string, with a local-dev fallback.</summary>
internal static class DrydockDatabase
{
    /// <summary>Local-dev fallback connection string used when none is configured.</summary>
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=drydock;Username=postgres;Password=postgres";

    /// <summary>Returns the configured Drydock connection string, or the local-dev fallback.</summary>
    public static string ConnectionString(IConfiguration configuration) =>
        configuration["ConnectionStrings:Drydock"] ?? DefaultConnectionString;
}

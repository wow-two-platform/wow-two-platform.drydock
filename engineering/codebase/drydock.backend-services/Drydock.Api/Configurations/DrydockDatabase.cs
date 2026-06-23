namespace Drydock.Api.Configurations;

/// <summary>Names the configuration key the Drydock database connection string is read from.</summary>
internal static class DrydockDatabase
{
    /// <summary>The configuration key holding the Drydock connection string (set in <c>appsettings.json</c>; env <c>DB_CONNECTION</c> overrides via the SDK persistence bundle).</summary>
    public const string ConnectionStringConfigKey = "ConnectionStrings:Drydock";
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Drydock.IntegrationTests.Harness;

/// <summary>
/// Generic web-API test host wrapping <see cref="WebApplicationFactory{TEntryPoint}"/> with conventional
/// defaults:
/// <list type="bullet">
///   <item>Environment forced to <c>"Production"</c> (the Drydock startup is not gated on env, so this mirrors prod).</item>
///   <item>The supplied connection string injected into <c>ConnectionStrings:&lt;ConnectionName&gt;</c> when set.</item>
///   <item>A <see cref="ConfigureServicesHook"/> for replacing services before the host builds (DB provider, auth, fakes).</item>
/// </list>
/// </summary>
/// <typeparam name="TEntryPoint">The application entry-point type (typically <c>Program</c>).</typeparam>
/// <remarks>
/// Host-agnostic over <typeparamref name="TEntryPoint"/> — nothing here is Drydock-specific, so it extracts
/// to the wow-two backend-beta SDK as the canonical <c>WebApiTestHost&lt;TEntryPoint&gt;</c>. The DB-lifecycle
/// concern lives in <see cref="DrydockAppFixture"/>, not here.
/// </remarks>
public class WebApiTestHost<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    /// <summary>
    /// The configuration key (under <c>ConnectionStrings</c>) the connection string is injected into.
    /// Defaults to <c>"Drydock"</c> — the key <c>Drydock.Persistence</c> reads.
    /// </summary>
    public string ConnectionName { get; init; } = "Drydock";

    /// <summary>
    /// Connection string injected as <c>ConnectionStrings:&lt;ConnectionName&gt;</c> when set. The host reads
    /// it through <see cref="IConfiguration"/>, so this overrides the in-code default connection string.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>
    /// Service-replacement hook, run via <see cref="WebApplicationFactory{TEntryPoint}"/>'s test-services
    /// stage (after the app's own registrations, so it wins). Use it to swap the DB provider, register a
    /// test authentication scheme, or stub adapters.
    /// </summary>
    public Action<IServiceCollection>? ConfigureServicesHook { get; init; }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production); // Drydock startup isn't env-gated — mirror prod.

        if (ConnectionString is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{ConnectionName}"] = ConnectionString,
                });
            });
        }

        // ConfigureTestServices runs AFTER the app's ConfigureServices, so replacements here win.
        builder.ConfigureTestServices(services => ConfigureServicesHook?.Invoke(services));
    }
}

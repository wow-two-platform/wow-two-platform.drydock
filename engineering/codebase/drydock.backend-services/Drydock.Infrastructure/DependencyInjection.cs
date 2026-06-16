using System.Net.Http.Headers;
using Drydock.Application.Abstractions;
using Drydock.Infrastructure.GitHub;
using Drydock.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drydock.Infrastructure;

/// <summary>
/// Registers the infrastructure layer. Today: the system clock and the GitHub REST client. Next:
/// SSH executor (SSH.NET), Hetzner Cloud / Porkbun / Cloudflare API clients, and the AES secret cipher.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds infrastructure services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration; // reserved — options binding for the SSH/registrar/DNS adapters lands here.

        services.AddSingleton<IClock, SystemClock>();

        // The GitHub adapter reads the signed-in admin's OAuth token off the current request.
        services.AddHttpContextAccessor();

        // Typed client for the GitHub REST API. Constant headers live here; the per-request
        // Authorization (the user's OAuth token) is attached inside the adapter.
        services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Drydock");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });

        return services;
    }
}

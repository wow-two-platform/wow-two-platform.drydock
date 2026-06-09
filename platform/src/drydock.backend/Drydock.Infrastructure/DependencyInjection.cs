using Drydock.Application.Abstractions;
using Drydock.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drydock.Infrastructure;

/// <summary>
/// Registers the infrastructure layer. Today: the system clock. Next: SSH executor (SSH.NET),
/// Hetzner Cloud / Porkbun / Cloudflare API clients, and the AES secret cipher.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds infrastructure services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = configuration; // reserved — options binding for the SSH/registrar/DNS adapters lands here.

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}

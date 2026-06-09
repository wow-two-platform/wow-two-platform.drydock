using Microsoft.Extensions.DependencyInjection;

namespace Drydock.Application;

/// <summary>Registers application-layer services — MediatR request handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds MediatR and scans this assembly for command/query handlers.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}

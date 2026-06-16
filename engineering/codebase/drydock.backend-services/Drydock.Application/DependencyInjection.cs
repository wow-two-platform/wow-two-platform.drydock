using Microsoft.Extensions.DependencyInjection;
using WoW.Two.Sdk.Backend.Beta.Mediator;

namespace Drydock.Application;

/// <summary>Registers application-layer services — the mediator + its CQRS handlers.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the mediator and scans this assembly for command/query handlers.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(typeof(DependencyInjection).Assembly);
        return services;
    }
}

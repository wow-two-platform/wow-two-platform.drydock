using System.Text.Json.Serialization;
using Drydock.Application;
using Drydock.Infrastructure;
using Drydock.Persistence;

namespace Drydock.Api.Configurations;

/// <summary>Per-concern host registration extensions for the Drydock API.</summary>
public static class HostConfigurationExtensions
{
    /// <summary>Loads optional local settings overrides (gitignored).</summary>
    public static WebApplicationBuilder AddSettings(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
        return builder;
    }

    /// <summary>Registers the persistence layer (Npgsql EF Core context + bespoke SQL migrator + stores).</summary>
    public static WebApplicationBuilder AddPersistenceLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddPersistence(builder.Configuration);
        return builder;
    }

    /// <summary>Registers the infrastructure layer (clock, and later SSH/registrar/DNS adapters).</summary>
    public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder;
    }

    /// <summary>Registers the application layer (MediatR command/query handlers).</summary>
    public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddApplication();
        return builder;
    }

    /// <summary>Registers API services — controllers + enum-as-string JSON (OpenAPI is wired by <c>AddApiDefaults</c>).</summary>
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return builder;
    }
}

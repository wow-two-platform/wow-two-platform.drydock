using Wheelhouse.Application;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Persistence;
using Wheelhouse.Persistence.Repositories;
using WoW.Two.Sdk.Backend.Beta.Data;
using WoW.Two.Sdk.Backend.Beta.Foundation.Time;
using WoW.Two.Sdk.Backend.Beta.Foundation.Validation;
using WoW.Two.Sdk.Backend.Beta.Integrations;
using WoW.Two.Sdk.Backend.Beta.Integrations.Ghcr;
using WoW.Two.Sdk.Backend.Beta.Integrations.GitHub;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Validation;
using WoW.Two.Sdk.Backend.Beta.Meta;
using WoW.Two.Sdk.Backend.Beta.Web.Json;

namespace Wheelhouse.Api.Configurations;

/// <summary>Per-concern host registration extensions for the Wheelhouse API.</summary>
public static class HostConfigurationExtensions
{
    /// <summary>Loads optional local settings overrides (gitignored).</summary>
    public static WebApplicationBuilder AddSettings(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
        builder.AddDeploymentHosting();
        return builder;
    }

    /// <summary>Configures the SDK boot floor for a single-user control plane — output cache, rate limiting, and OTLP export off; OpenAPI in development only.</summary>
    public static WebApplicationBuilder AddPlatformDefaults(this WebApplicationBuilder builder)
    {
        builder.AddApiDefaults(o =>
        {
            o.ServiceName = "wheelhouse";
            o.EnableOutputCache = false;
            o.EnableRateLimiting = false;
            o.EnableOtlpExporters = false;
            o.ExposeOpenApi = builder.Environment.IsDevelopment();
        });
        return builder;
    }

    /// <summary>Registers the persistence layer — the SDK one-call Postgres bundle (data source, connection factory, audit interceptor, snake_case EF context, embedded bespoke migrator) plus the Wheelhouse stores.</summary>
    public static WebApplicationBuilder AddPersistenceLayer(this WebApplicationBuilder builder)
    {
        // One-call host floor for the context: resolves the connection string, builds the shared NpgsqlDataSource,
        // registers the Dapper connection factory + audit interceptor, adds the snake_case EF context, and wires the
        // embedded bespoke migrator over typeof(WheelhouseDbContext).Assembly. Keep Wheelhouse's existing config key
        // (ConnectionStrings:Wheelhouse) instead of the SDK default (DatabaseOptions:ConnectionString); env DB_CONNECTION still overrides.
        builder.Services.AddPostgresPersistence<WheelhouseDbContext>(
            builder.Configuration,
            o => o.ConnectionStringConfigKey = WheelhouseDatabase.ConnectionStringConfigKey);

        builder.Services.AddScoped<IServerRepository, EfServerRepository>();
        builder.Services.AddScoped<IProductRepository, EfProductRepository>();

        return builder;
    }

    /// <summary>Registers the infrastructure layer — the SDK time provider, the GitHub and GHCR integration clients, and the OAuth-token source (SSH/registrar/DNS adapters land here next).</summary>
    public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddTimeProviders();
        builder.Services.AddSingleton(builder.Configuration.GetSection("DeploymentRunner")
            .Get<Wheelhouse.Infrastructure.Settings.DeploymentSettings>() ?? new());
        builder.Services.AddScoped<IDeploymentGateway, Wheelhouse.Infrastructure.Deployments.DeploymentGateway>();

        // The integration clients read the signed-in admin's OAuth token off the current request.
        builder.Services.AddHttpContextAccessTokenProvider();
        builder.Services.AddGitHubIntegration();
        builder.Services.AddGhcrIntegration();

        return builder;
    }

    /// <summary>Registers the application layer — the mediator with its validation behavior, scanning the application assembly for handlers and validators.</summary>
    public static WebApplicationBuilder AddApplicationLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediator(typeof(IApplicationMarker).Assembly);
        builder.Services.AddMediatorValidationBehavior();
        builder.Services.AddFluentValidatorsFromAssemblies(typeof(IApplicationMarker).Assembly);

        return builder;
    }

    /// <summary>Registers API services — controllers + enum-as-string JSON via the SDK helper (OpenAPI is wired by <c>AddApiDefaults</c>).</summary>
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers()
            .AddJsonStringEnums();

        return builder;
    }
}

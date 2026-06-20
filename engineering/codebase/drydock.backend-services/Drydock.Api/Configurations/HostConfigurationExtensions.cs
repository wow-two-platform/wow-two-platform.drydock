using System.Data.Common;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using Drydock.Application;
using Drydock.Application.Abstractions;
using Drydock.Infrastructure.GitHub;
using Drydock.Persistence;
using Drydock.Persistence.Stores;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WoW.Two.Sdk.Backend.Beta.Data.Dapper;
using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Backend.Beta.Foundation.Time;
using WoW.Two.Sdk.Backend.Beta.Foundation.Validation;
using WoW.Two.Sdk.Backend.Beta.Mediator;
using WoW.Two.Sdk.Backend.Beta.Mediator.Validation;
using WoW.Two.Sdk.Backend.Beta.Meta;

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

    /// <summary>Configures the SDK boot floor for a single-user control plane — output cache, rate limiting, and OTLP export off; OpenAPI in development only.</summary>
    public static WebApplicationBuilder AddPlatformDefaults(this WebApplicationBuilder builder)
    {
        builder.AddApiDefaults(o =>
        {
            o.ServiceName = "drydock";
            o.EnableOutputCache = false;
            o.EnableRateLimiting = false;
            o.EnableOtlpExporters = false;
            o.ExposeOpenApi = builder.Environment.IsDevelopment();
        });
        return builder;
    }

    /// <summary>Registers the persistence layer — Npgsql data source, EF Core context (pure mapper), stores, and the bespoke SQL migrator.</summary>
    public static WebApplicationBuilder AddPersistenceLayer(this WebApplicationBuilder builder)
    {
        // Shared Npgsql data source: EF resolves the concrete NpgsqlDataSource; the migrator binds to it as a base DbDataSource.
        builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(DrydockDatabase.ConnectionString(builder.Configuration)).Build());
        builder.Services.AddSingleton<DbDataSource>(sp => sp.GetRequiredService<NpgsqlDataSource>());

        // SDK connection seam: DataSourceConnectionFactory as IDbConnectionFactory, backed by the DbDataSource above.
        builder.Services.AddDataSourceConnectionFactory();

        builder.Services.AddDbContext<DrydockDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
                   .UseSnakeCaseNamingConvention());

        builder.Services.AddScoped<IServerStore, EfServerStore>();
        builder.Services.AddScoped<IProductStore, EfProductStore>();

        // SQL migrator (embedded source) — owns the Postgres schema; EF is a pure mapper over it.
        builder.Services.AddDatabaseBespokeMigrations(typeof(DrydockDbContext).Assembly);

        return builder;
    }

    /// <summary>Registers the infrastructure layer — the SDK time provider and the typed GitHub and GHCR clients (SSH/registrar/DNS adapters land here next).</summary>
    public static WebApplicationBuilder AddInfrastructureLayer(this WebApplicationBuilder builder)
    {
        builder.Services.AddTimeProviders();

        // The GitHub adapter reads the signed-in admin's OAuth token off the current request.
        builder.Services.AddHttpContextAccessor();

        // Typed client for the GitHub REST API. Constant headers live here; the per-request
        // Authorization (the user's OAuth token) is attached inside the adapter.
        builder.Services.AddHttpClient<IGitHubClient, GitHubClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Drydock");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });

        // Typed client for the GitHub Container Registry (GHCR) v2 API — confirms a published image tag
        // exists. Calls absolute ghcr.io URLs (token endpoint and manifest), so no base address.
        builder.Services.AddHttpClient<IContainerRegistryClient, GhcrClient>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Drydock");
        });

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

    /// <summary>Registers API services — controllers + enum-as-string JSON (OpenAPI is wired by <c>AddApiDefaults</c>).</summary>
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return builder;
    }
}

using Drydock.Api.Auth;
using WoW.Two.Sdk.Backend.Beta.Meta;

namespace Drydock.Api.Configurations;

/// <summary>Configures the Drydock host — a slim orchestrator that delegates to per-concern extensions.</summary>
public static class HostConfiguration
{
    /// <summary>Registers services across all layers.</summary>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        // Local-only dev overrides first — visible to AddApiDefaults' config reads.
        builder.AddSettings();

        builder.AddPlatformDefaults();

        builder
            .AddPersistenceLayer()
            .AddInfrastructureLayer()
            .AddApplicationLayer()
            .AddAuthentication()
            .AddApiServices();

        return builder;
    }

    /// <summary>Configures the middleware pipeline and maps endpoints.</summary>
    public static WebApplication Configure(this WebApplication app)
    {
        // TEMP: activate the SDK's registered exception handlers (validation → 400 ProblemDetails). Drop once
        // UseApiDefaults wires UseExceptionHandler itself (this branch's SDK fix, in the next publish).
        app.UseExceptionHandler();

        // Serve the React dashboard from wwwroot (single-deploy: API + UI in one host). Static assets stay public so the
        // sign-in screen loads before auth, and are registered before the SDK pipeline so they short-circuit.
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // SDK pipeline: forwarded headers, secure headers, response compression; maps OpenAPI (dev) + the SDK health probe.
        app.UseApiDefaults();

        // Auth after static files + the SDK pipeline, before endpoints — the fallback policy gates the controllers while
        // the SPA shell and the anonymous /health below stay reachable.
        app.UseDrydockAuth();

        app.MapControllers();

        // An unmatched /api/* must 404 as JSON — never fall through to the SPA shell. An HTML body for an API path is
        // cacheable and breaks clients (it caused the products cache-confusion bug).
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"))
            .AllowAnonymous();
        app.MapFallbackToFile("index.html").AllowAnonymous();

        return app;
    }
}

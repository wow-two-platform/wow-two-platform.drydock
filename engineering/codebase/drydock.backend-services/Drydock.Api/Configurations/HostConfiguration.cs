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

        // SDK boot floor: structured logging, tracing, metrics, health, ProblemDetails, secure headers, compression, OpenAPI.
        // Output cache + rate limiting OFF — single-user control plane with cookie-scoped responses; the edge proxy owns throttling.
        // OTLP exporters OFF — no collector yet (tracing still fills Activity, so ProblemDetails stays trace-correlated). OpenAPI dev-only.
        builder.AddApiDefaults(o =>
        {
            o.ServiceName = "drydock";
            o.EnableOutputCache = false;
            o.EnableRateLimiting = false;
            o.EnableOtlpExporters = false;
            o.ExposeOpenApi = builder.Environment.IsDevelopment();

            // TEMP: the published SDK maps /health WITHOUT AllowAnonymous, and Drydock's default-deny fallback would 401 it.
            // Park the SDK probe off the canonical path; the anonymous /health below stays authoritative. Once the SDK ships
            // anonymous health (this branch's fix), drop the /health MapGet and reset this to the default "/health".
            o.HealthEndpointPath = "/_health";
        });

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

        // Canonical liveness — anonymous so external probes (LB / Docker / Traefik) reach it under the default-deny fallback.
        // TEMP bridge until the SDK health endpoint ships AllowAnonymous (see HealthEndpointPath note above), then removed.
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Drydock" }))
            .AllowAnonymous();

        // An unmatched /api/* must 404 as JSON — never fall through to the SPA shell. An HTML body for an API path is
        // cacheable and breaks clients (it caused the products cache-confusion bug).
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"))
            .AllowAnonymous();
        app.MapFallbackToFile("index.html").AllowAnonymous();

        return app;
    }
}

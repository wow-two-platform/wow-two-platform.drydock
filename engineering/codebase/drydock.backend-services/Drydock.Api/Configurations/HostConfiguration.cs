using Drydock.Api.Auth;

namespace Drydock.Api.Configurations;

/// <summary>Configures the Drydock host — a slim orchestrator that delegates to per-concern extensions.</summary>
public static class HostConfiguration
{
    /// <summary>Registers services across all layers.</summary>
    public static WebApplicationBuilder Configure(this WebApplicationBuilder builder)
    {
        builder
            .AddSettings()
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
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        // Serve the React dashboard from wwwroot (single-deploy: API + UI in one host).
        // Static assets stay public so the sign-in screen itself can load before auth.
        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Auth runs after routing/static files, before endpoints — so [Authorize] + the fallback
        // policy gate the controllers (Servers etc.) while /health and the SPA shell stay reachable.
        app.UseDrydockAuth();

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Drydock" }))
            .AllowAnonymous();

        // An unmatched /api/* must 404 as JSON — never fall through to the SPA shell. An HTML body for
        // an API path is cacheable and breaks clients (it caused the products cache-confusion bug).
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Not Found"))
            .AllowAnonymous();
        app.MapFallbackToFile("index.html").AllowAnonymous();

        return app;
    }
}

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
            .AddApiServices();

        return builder;
    }

    /// <summary>Configures the middleware pipeline and maps endpoints.</summary>
    public static WebApplication Configure(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        // Serve the React dashboard from wwwroot (single-deploy: API + UI in one host).
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Drydock" }));
        app.MapFallbackToFile("index.html");

        return app;
    }
}

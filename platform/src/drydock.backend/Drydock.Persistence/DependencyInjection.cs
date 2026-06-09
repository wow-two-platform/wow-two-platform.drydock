using Drydock.Application.Abstractions;
using Drydock.Persistence.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Drydock.Persistence;

/// <summary>Registers the persistence layer — EF Core SQLite context and stores.</summary>
public static class DependencyInjection
{
    /// <summary>Adds the <see cref="DrydockDbContext"/> and store implementations.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:Drydock"] ?? "Data Source=drydock.db";

        services.AddDbContext<DrydockDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IServerStore, EfServerStore>();

        return services;
    }

    /// <summary>Applies any pending EF Core migrations, creating the full schema on a fresh database.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DrydockDbContext>();
        await db.Database.MigrateAsync();
    }
}

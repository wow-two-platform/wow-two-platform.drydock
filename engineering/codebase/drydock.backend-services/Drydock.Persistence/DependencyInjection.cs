using System.Data.Common;
using Drydock.Application.Abstractions;
using Drydock.Persistence.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WoW.Two.Sdk.Backend.Beta.Data.Dapper;
using WoW.Two.Sdk.Backend.Beta.Data.Migrations.Bespoke;

namespace Drydock.Persistence;

/// <summary>Registers the persistence layer — Npgsql data source, EF Core context (pure mapper), and the bespoke SQL migrator.</summary>
public static class DependencyInjection
{
    /// <summary>Local-dev fallback connection string used when none is configured.</summary>
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Database=drydock;Username=postgres;Password=postgres";

    /// <summary>Adds the <see cref="DrydockDbContext"/>, stores, and the Postgres bespoke migrator over the embedded SQL.</summary>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Shared Npgsql data source: EF resolves the concrete NpgsqlDataSource; the migrator binds to it as a base DbDataSource.
        services.AddSingleton(_ =>
        {
            var connectionString = configuration["ConnectionStrings:Drydock"] ?? DefaultConnectionString;
            return new NpgsqlDataSourceBuilder(connectionString).Build();
        });
        services.AddSingleton<DbDataSource>(sp => sp.GetRequiredService<NpgsqlDataSource>());

        // SDK connection seam: DataSourceConnectionFactory as IDbConnectionFactory, backed by the DbDataSource above.
        services.AddDataSourceConnectionFactory();

        services.AddDbContext<DrydockDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IServerStore, EfServerStore>();
        services.AddScoped<IProductStore, EfProductStore>();

        // SQL migrator (embedded source) — owns the Postgres schema; EF is a pure mapper over it.
        services.AddDatabaseBespokeMigrations(typeof(DrydockDbContext).Assembly);

        return services;
    }

    /// <summary>Ensures the target database exists, then applies pending SQL migrations — creating the database and the full schema on first run.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        // CREATE DATABASE can't run inside the target DB — the dialect connects to the maintenance DB first.
        // Read the raw string from configuration: NpgsqlDataSource.ConnectionString redacts the password.
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration["ConnectionStrings:Drydock"] ?? DefaultConnectionString;
        var dialect = scope.ServiceProvider.GetRequiredService<IMigrationDialect>();
        await dialect.EnsureDatabaseExistsAsync(connectionString);

        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunnerService>();
        await runner.ApplyPendingAsync("startup");
    }
}

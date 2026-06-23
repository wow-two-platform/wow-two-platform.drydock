using Drydock.Domain.Deployments.Entities;
using Drydock.Domain.Domains.Entities;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Secrets.Entities;
using Drydock.Domain.Servers.Entities;
using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Data.EntityFrameworkCore.Naming;
using WoW.Two.Sdk.Backend.Beta.Data.EntityFrameworkCore.Sqlite;

namespace Drydock.Persistence;

/// <summary>EF Core context for the Drydock control plane — a pure mapper over the Postgres schema the bespoke SQL migrator owns. Snake_case naming + enums-as-snake_case-text; <c>DateTimeOffset</c> → <c>timestamptz</c> natively.</summary>
public sealed class DrydockDbContext(DbContextOptions<DrydockDbContext> options) : DbContext(options)
{
    /// <summary>The EF Core SQLite provider name — gates the SQLite-only <c>DateTimeOffset</c> binary conversion (test hosts only; Npgsql maps <c>DateTimeOffset</c> natively).</summary>
    private const string SqliteProviderName = "Microsoft.EntityFrameworkCore.Sqlite";

    /// <summary>Gets the registered deploy-target servers.</summary>
    public DbSet<Server> Servers => Set<Server>();

    /// <summary>Gets the portfolio products.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Gets the deployment history.</summary>
    public DbSet<Deployment> Deployments => Set<Deployment>();

    /// <summary>Gets the managed domains.</summary>
    public DbSet<ManagedDomain> Domains => Set<ManagedDomain>();

    /// <summary>Gets the encrypted secrets.</summary>
    public DbSet<SecretEntry> Secrets => Set<SecretEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Server>(e =>
        {
            e.ToTable("servers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Host).IsUnique();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Host).IsRequired();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).IsRequired();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Repo).IsRequired();
        });

        modelBuilder.Entity<Deployment>(e =>
        {
            e.ToTable("deployments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProductId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<ManagedDomain>(e =>
        {
            e.ToTable("domains");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).IsRequired();
        });

        modelBuilder.Entity<SecretEntry>(e =>
        {
            e.ToTable("secrets");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Scope, x.RefId, x.Key }).IsUnique();
            e.Property(x => x.Key).IsRequired();
        });

        // Store every enum in the model as snake_case text via the SDK reversible converter (member-built reverse map →
        // multi-word values round-trip losslessly, e.g. RolledBack ↔ rolled_back). One call replaces the per-enum list;
        // runs after the entities are mapped. Postgres-native casing; text columns unchanged.
        modelBuilder.ApplyEnumStringConversions();

        // SQLite has no native DateTimeOffset (Npgsql maps it natively) — under the SQLite test provider, store every
        // DateTimeOffset as a binary Int64 so range reads and ORDER BY match Postgres. No-op under Npgsql.
        if (Database.ProviderName == SqliteProviderName)
            modelBuilder.ApplyDateTimeOffsetToBinaryConversion();
    }
}

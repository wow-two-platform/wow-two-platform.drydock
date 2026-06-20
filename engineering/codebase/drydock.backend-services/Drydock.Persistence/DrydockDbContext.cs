using Drydock.Domain.Deployments.Entities;
using Drydock.Domain.Domains.Entities;
using Drydock.Domain.Products.Entities;
using Drydock.Domain.Secrets.Entities;
using Drydock.Domain.Servers.Entities;
using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Data.EntityFrameworkCore.Naming;

namespace Drydock.Persistence;

/// <summary>EF Core context for the Drydock control plane — a pure mapper over the Postgres schema the bespoke SQL migrator owns. Snake_case naming + enums-as-snake_case-text; <c>DateTimeOffset</c> → <c>timestamptz</c> natively.</summary>
public sealed class DrydockDbContext(DbContextOptions<DrydockDbContext> options) : DbContext(options)
{
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
        // Each enum is stored as snake_case text via the SDK's reversible converter (member-built reverse map →
        // multi-word values round-trip losslessly, e.g. RolledBack ↔ rolled_back). Applied per-property because
        // EnumCaseConverter has no parameterless ctor, so the model-level Properties<T>().HaveConversion<T>() can't build it.
        modelBuilder.Entity<Server>(e =>
        {
            e.ToTable("servers");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Host).IsUnique();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Host).IsRequired();
            e.Property(x => x.Status).HasEnumStringConversion();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).IsRequired();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Repo).IsRequired();
            e.Property(x => x.Status).HasEnumStringConversion();
        });

        modelBuilder.Entity<Deployment>(e =>
        {
            e.ToTable("deployments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ProductId, x.CreatedAtUtc });
            e.Property(x => x.Status).HasEnumStringConversion();
        });

        modelBuilder.Entity<ManagedDomain>(e =>
        {
            e.ToTable("domains");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Status).HasEnumStringConversion();
        });

        modelBuilder.Entity<SecretEntry>(e =>
        {
            e.ToTable("secrets");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Scope, x.RefId, x.Key }).IsUnique();
            e.Property(x => x.Key).IsRequired();
            e.Property(x => x.Scope).HasEnumStringConversion();
        });
    }
}

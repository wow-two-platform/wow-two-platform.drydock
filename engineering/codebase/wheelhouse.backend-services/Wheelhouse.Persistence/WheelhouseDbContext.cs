using Wheelhouse.Domain.Audit.Entities;
using Wheelhouse.Domain.Deployments.Entities;
using Wheelhouse.Domain.Operations.Entities;
using Wheelhouse.Domain.Domains.Entities;
using Wheelhouse.Domain.Products.Entities;
using Wheelhouse.Domain.Secrets.Entities;
using Wheelhouse.Domain.Servers.Entities;
using Microsoft.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Data.EntityFrameworkCore;
using WoW.Two.Sdk.Backend.Beta.Data.EntityFrameworkCore.Naming;
using WoW.Two.Sdk.Backend.Beta.Data.EntityFrameworkCore.Sqlite;

namespace Wheelhouse.Persistence;

/// <summary>EF Core context for the Wheelhouse control plane — a pure mapper over the Postgres schema the bespoke SQL migrator owns. Snake_case naming + enums-as-snake_case-text; <c>DateTimeOffset</c> → <c>timestamptz</c> natively. On the SDK <see cref="AppDbContextBase"/>, so the audit interceptor stamps the <c>IAuditable</c> create/update timestamps.</summary>
public sealed class WheelhouseDbContext(DbContextOptions<WheelhouseDbContext> options) : AppDbContextBase(options)
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

    /// <summary>Gets the append-only, hash-chained audit trail.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>Gets thirty days of host and container readings.</summary>
    public DbSet<VitalsSample> VitalsSamples => Set<VitalsSample>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Base applies the SDK conventions and this assembly's IEntityTypeConfiguration<T> (none here — harmless no-op).
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Server>(e =>
        {
            e.ToTable(Server.TableName);
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Host).IsUnique();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Host).IsRequired();
            // IAuditable maps onto the existing schema-first columns (no rename → no migration for created_at_utc).
            e.Property(x => x.CreatedAt).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable(Product.TableName);
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).IsRequired();
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Repo).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<Deployment>(e =>
        {
            e.ToTable(Deployment.TableName);
            e.HasKey(x => x.Id);
            e.Property(x => x.CreatedAt).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at_utc");
            e.HasIndex(x => new { x.ProductId, x.CreatedAt });
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

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.ToTable(AuditEntry.TableName);
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Sequence).IsUnique();
            e.HasIndex(x => x.OccurredAtUtc);
            e.Property(x => x.PreviousHash).IsRequired();
            e.Property(x => x.Hash).IsRequired();
            e.Property(x => x.Actor).IsRequired();
            e.Property(x => x.Action).IsRequired();
            e.Property(x => x.Subject).IsRequired();
        });

        modelBuilder.Entity<VitalsSample>(e =>
        {
            e.ToTable(VitalsSample.TableName);
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TargetId, x.SampledAtUtc });
            e.HasIndex(x => x.SampledAtUtc);
            e.Property(x => x.TargetId).IsRequired();
            e.Property(x => x.ServerId).IsRequired();
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

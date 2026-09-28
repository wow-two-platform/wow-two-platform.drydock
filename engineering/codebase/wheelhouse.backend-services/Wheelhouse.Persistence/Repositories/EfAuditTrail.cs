using Microsoft.EntityFrameworkCore;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Application.Audit;
using Wheelhouse.Domain.Audit.Entities;
using WoW.Two.Sdk.Backend.Beta.Foundation.Audit;

namespace Wheelhouse.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IAuditTrail"/>: one transaction per append, serialized so each entry
/// chains to the one committed before it.</summary>
internal sealed class EfAuditTrail(
    WheelhouseDbContext db, IHashChainSealer<AuditEntry> sealer, IHashChainVerifier<AuditEntry> verifier, TimeProvider time)
    : IAuditTrail
{
    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    // Any constant works; it only has to differ from other advisory locks the database uses.
    private const long AppendLockKey = 0x57484155444954;

    /// <inheritdoc />
    public async Task AppendAsync(AuditRecord record, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (db.Database.ProviderName == PostgresProviderName)
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [AppendLockKey], ct);
        var last = await db.AuditEntries.AsNoTracking().OrderByDescending(e => e.Sequence).FirstOrDefaultAsync(ct);
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            // Microsecond precision is what Postgres keeps; hashing the stored value keeps the chain verifiable.
            OccurredAtUtc = TruncateToMicroseconds(time.GetUtcNow()),
            Actor = record.Actor,
            Action = record.Action,
            Subject = record.Subject,
            Outcome = record.Outcome,
            Detail = record.Detail,
            Reason = record.Reason
        };
        sealer.Seal(entry, last);
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEntry>> ListAsync(int limit, long? before, CancellationToken ct = default) =>
        await db.AuditEntries.AsNoTracking()
            .Where(e => before == null || e.Sequence < before)
            .OrderByDescending(e => e.Sequence)
            .Take(limit)
            .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<AuditVerification> VerifyAsync(CancellationToken ct = default)
    {
        var entries = await db.AuditEntries.AsNoTracking().OrderBy(e => e.Sequence).ToListAsync(ct);
        var result = verifier.Verify(entries);
        return result.IsIntact
            ? new AuditVerification(true, entries.Count, null, null)
            : new AuditVerification(false, entries.Count, result.BrokenSequence, result.Reason.ToString());
    }

    private static DateTimeOffset TruncateToMicroseconds(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % 10, value.Offset);
}

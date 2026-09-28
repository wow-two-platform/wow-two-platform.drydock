using Microsoft.EntityFrameworkCore;
using Wheelhouse.Application.Abstractions;
using Wheelhouse.Domain.Operations.Entities;

namespace Wheelhouse.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IVitalsHistory"/>.</summary>
internal sealed class EfVitalsHistory(WheelhouseDbContext db) : IVitalsHistory
{
    /// <inheritdoc />
    public async Task AppendAsync(IReadOnlyList<VitalsSample> samples, CancellationToken ct = default)
    {
        if (samples.Count == 0)
            return;
        db.VitalsSamples.AddRange(samples);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VitalsSample>> ListAsync(string? targetId, DateTimeOffset since, CancellationToken ct = default) =>
        await db.VitalsSamples.AsNoTracking()
            .Where(s => s.SampledAtUtc >= since && (targetId == null || s.TargetId == targetId))
            .OrderBy(s => s.SampledAtUtc)
            .ThenBy(s => s.TargetId)
            .ToListAsync(ct);

    /// <inheritdoc />
    public Task<int> PruneAsync(DateTimeOffset before, CancellationToken ct = default) =>
        db.VitalsSamples.Where(s => s.SampledAtUtc < before).ExecuteDeleteAsync(ct);
}

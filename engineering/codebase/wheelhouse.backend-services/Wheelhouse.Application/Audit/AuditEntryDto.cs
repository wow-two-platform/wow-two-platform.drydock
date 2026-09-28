using Wheelhouse.Domain.Audit.Entities;
using Wheelhouse.Domain.Audit.Enums;

namespace Wheelhouse.Application.Audit;

/// <summary>One audit entry as the dashboard shows it; the chain hashes stay server-side.</summary>
/// <param name="Sequence">The entry's position in the chain.</param>
/// <param name="OccurredAt">The UTC instant the action ended.</param>
/// <param name="Actor">The operator's login.</param>
/// <param name="Action">The action's stable name.</param>
/// <param name="Subject">What the action acted on.</param>
/// <param name="Outcome">How the action ended.</param>
/// <param name="Detail">Operator-safe context.</param>
/// <param name="Reason">Why a failed action failed.</param>
public sealed record AuditEntryDto(
    long Sequence, DateTimeOffset OccurredAt, string Actor, string Action, string Subject, AuditOutcome Outcome,
    string? Detail, string? Reason)
{
    /// <summary>Maps a stored entry, leaving its hashes behind.</summary>
    public static AuditEntryDto From(AuditEntry entry) => new(entry.Sequence, entry.OccurredAtUtc, entry.Actor,
        entry.Action, entry.Subject, entry.Outcome, entry.Detail, entry.Reason);
}

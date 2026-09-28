using Wheelhouse.Domain.Audit.Enums;

namespace Wheelhouse.Application.Audit;

/// <summary>One finished operator action, before the audit trail stamps its time and chains it.</summary>
/// <param name="Actor">The signed-in operator's login.</param>
/// <param name="Action">The action's stable name.</param>
/// <param name="Subject">What the action acted on.</param>
/// <param name="Outcome">How the action ended.</param>
/// <param name="Detail">Operator-safe context.</param>
/// <param name="Reason">Why a failed action failed.</param>
public sealed record AuditRecord(
    string Actor, string Action, string Subject, AuditOutcome Outcome, string? Detail, string? Reason);

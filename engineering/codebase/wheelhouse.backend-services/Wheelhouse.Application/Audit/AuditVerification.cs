namespace Wheelhouse.Application.Audit;

/// <summary>The result of recomputing the audit chain. An intact chain shows that no stored entry was edited, reordered
/// or removed from its middle; it cannot show that the newest entries were not truncated.</summary>
/// <param name="Intact">Whether every entry and link verified.</param>
/// <param name="Entries">How many entries were checked.</param>
/// <param name="BrokenSequence">The first entry that failed, when the chain is broken.</param>
/// <param name="Reason">Why that entry failed: a hash mismatch, a broken link or a sequence gap.</param>
public sealed record AuditVerification(bool Intact, int Entries, long? BrokenSequence, string? Reason);

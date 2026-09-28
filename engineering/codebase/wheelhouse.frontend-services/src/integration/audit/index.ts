import type { AuditEntry, AuditVerification } from "@/domain/audit";
import { requestData } from "@/integration/common";
import { AuditEntrySchema, AuditVerificationSchema } from "./schemas";

/** The append-only audit trail; nothing here changes it. */
export const auditApi = {
  /** The newest `limit` entries (1-200), or those before one sequence number to page back. */
  list: (limit: number, before?: number | null, signal?: AbortSignal) =>
    requestData<AuditEntry[]>(
      `/api/audit?limit=${limit}` + (before ? `&before=${before}` : ""),
      AuditEntrySchema.array(),
      { signal },
    ),

  verify: (signal?: AbortSignal) =>
    requestData<AuditVerification>("/api/audit/verification", AuditVerificationSchema, { signal }),
};

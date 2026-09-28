import { z } from "zod";
import { AuditOutcome } from "@/domain/audit";

export const AuditEntrySchema = z.object({
  sequence: z.number().int(),
  occurredAt: z.string(),
  actor: z.string(),
  action: z.string(),
  subject: z.string(),
  outcome: z.enum(AuditOutcome),
  detail: z.string().nullable().optional(),
  reason: z.string().nullable().optional(),
});
export const AuditVerificationSchema = z.object({
  intact: z.boolean(),
  entries: z.number().int(),
  brokenSequence: z.number().int().nullable().optional(),
  reason: z.string().nullable().optional(),
});

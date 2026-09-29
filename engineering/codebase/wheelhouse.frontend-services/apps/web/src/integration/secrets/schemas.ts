import { z } from "zod";
import { SecretState, VaultStatus } from "@/domain/secrets";

export const VaultSummarySchema = z.object({
  id: z.string(),
  name: z.string(),
  serverId: z.string(),
  status: z.enum(VaultStatus),
});
export const VaultNamespaceSchema = z.object({
  slug: z.string(),
  name: z.string(),
  createdAtUtc: z.string(),
});
export const SecretMetadataSchema = z.object({
  namespace: z.string(),
  key: z.string(),
  state: z.enum(SecretState),
  version: z.number(),
  description: z.string().nullable().optional(),
  updatedAtUtc: z.string(),
});
export const VaultTokenSchema = z.object({
  id: z.string(),
  name: z.string(),
  createdAtUtc: z.string(),
  expiresAtUtc: z.string().nullable(),
  isRevoked: z.boolean(),
});
export const MintedTokenSchema = z.object({
  id: z.string(),
  name: z.string(),
  namespace: z.string(),
  token: z.string(),
});
export const VaultHygieneSchema = z.object({
  vault: z.string(),
  namespaces: z.number(),
  secrets: z.number(),
  disabledSecrets: z.number(),
  tokens: z.number(),
  secretRotationDays: z.number(),
  tokenRotationDays: z.number(),
  overdueSecrets: z.array(
    z.object({
      namespace: z.string(),
      key: z.string(),
      updatedAtUtc: z.string(),
      ageDays: z.number(),
    }),
  ),
  overdueTokens: z.array(
    z.object({
      namespace: z.string(),
      id: z.string(),
      name: z.string(),
      createdAtUtc: z.string(),
      ageDays: z.number(),
      expiresAtUtc: z.string().nullable(),
      reason: z.enum(["expired", "expires soon", "rotation due"]),
    }),
  ),
});
// Commands consume no response payload; their success still requires the management data envelope.
export const AcknowledgementSchema = z.unknown();

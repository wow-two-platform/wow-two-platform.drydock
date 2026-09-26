import { z } from "zod";
import { JobStatus, TargetCondition } from "@/domain/deployments";

const jobStatus = z.enum(JobStatus);
const nullableText = z.string().nullable();
export const DeploymentJobSchema = z.object({
  id: z.string(),
  status: jobStatus,
  targetId: nullableText.optional(),
  bundleId: nullableText.optional(),
  release: nullableText.optional(),
  actor: nullableText.optional(),
  submittedAt: z.string().optional(),
  reason: nullableText.optional(),
  failure: nullableText.optional(),
  startedAt: z.string().optional(),
  completedAt: z.string().optional(),
  mutationStarted: z.boolean().optional(),
});
export const DeploymentTargetSchema = z.object({
  id: z.string(),
  product: z.string(),
  environment: z.string(),
  serverId: z.string(),
  provider: z.string(),
  host: z.string(),
});
export const ReleaseArtifactSchema = z.object({
  id: z.string(),
  product: z.string(),
  release: z.string(),
  prerelease: z.boolean(),
  publishedAt: z.string(),
  provider: z.string(),
});
const RolloutSchema = z.object({
  id: z.string(),
  release: z.string().optional(),
  status: jobStatus.optional(),
  actor: z.string().optional(),
  startedAt: z.string().optional(),
  completedAt: z.string().optional(),
  reason: z.string().optional(),
  mutationStarted: z.boolean().optional(),
});
export const TargetStateSchema = z.object({
  targetId: z.string(),
  project: z.string(),
  condition: z.enum(TargetCondition),
  current: RolloutSchema.nullable(),
  active: RolloutSchema.nullable(),
});
export const TargetCheckSchema = z
  .object({
    targetId: z.string(),
    ok: z.boolean(),
    checks: z.array(
      z.object({ name: z.string(), ok: z.boolean(), detail: z.string() }),
    ),
    // The runner embeds state before the transport adds the outer target identifier.
    state: TargetStateSchema.omit({ targetId: true })
      .extend({ targetId: z.string().optional() })
      .optional(),
  })
  .transform((result) => ({
    ...result,
    ...(result.state
      ? {
          state: {
            ...result.state,
            targetId: result.state.targetId ?? result.targetId,
          },
        }
      : {}),
  }));
const MeasuresSchema = z.object({
  since: z.string(),
  deploys: z.number(),
  succeeded: z.number(),
  failed: z.number(),
  refused: z.number(),
  pending: z.number(),
  successRate: z.number().nullable(),
  medianRolloutSeconds: z.number().nullable(),
  medianRecoverySeconds: z.number().nullable(),
});
export const DeploymentStatsSchema = MeasuresSchema.extend({
  windowDays: z.number(),
  previous: MeasuresSchema.nullable(),
  daily: z.array(
    z.object({
      date: z.string(),
      succeeded: z.number(),
      failed: z.number(),
      refused: z.number(),
    }),
  ),
  targets: z.array(
    z.object({
      targetId: z.string(),
      deploys: z.number(),
      succeeded: z.number(),
      failed: z.number(),
      lastStatus: jobStatus,
      lastRelease: nullableText.optional(),
      lastDeployAt: z.string(),
      failingSince: nullableText,
    }),
  ),
});

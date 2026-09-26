import type {
  DeploymentJob,
  DeploymentStats,
  DeploymentTarget,
  ReleaseArtifact,
  TargetCheck,
  TargetState,
} from "@/domain/deployments";
import { requestData } from "@/integration/common";
import {
  DeploymentJobSchema,
  DeploymentStatsSchema,
  DeploymentTargetSchema,
  ReleaseArtifactSchema,
  TargetCheckSchema,
  TargetStateSchema,
} from "./schemas";

/** Deployment operations; every write carries its explicit action header. */
export const deploymentsApi = {
  listHistory: (signal?: AbortSignal) =>
    requestData<DeploymentJob[]>(
      "/api/deployments",
      DeploymentJobSchema.array(),
      { signal },
    ),

  listTargets: (signal?: AbortSignal) =>
    requestData<DeploymentTarget[]>(
      "/api/deployments/targets",
      DeploymentTargetSchema.array(),
      { signal },
    ),

  listReleases: (signal?: AbortSignal) =>
    requestData<ReleaseArtifact[]>(
      "/api/deployments/releases",
      ReleaseArtifactSchema.array(),
      { signal },
    ),

  getStats: (days: number, signal?: AbortSignal) =>
    requestData<DeploymentStats>(
      `/api/deployments/stats?days=${days}`,
      DeploymentStatsSchema,
      { signal },
    ),

  getTargetState: (target: string, signal?: AbortSignal) =>
    requestData<TargetState>(
      `/api/deployments/targets/${encodeURIComponent(target)}/state`,
      TargetStateSchema,
      { signal },
    ),

  checkTarget: (target: string, release?: string, signal?: AbortSignal) =>
    requestData<TargetCheck>(
      `/api/deployments/targets/${encodeURIComponent(target)}/check` +
        (release ? `?release=${encodeURIComponent(release)}` : ""),
      TargetCheckSchema,
      { signal },
    ),

  getOutcome: (id: string, signal?: AbortSignal) =>
    requestData<DeploymentJob>(
      `/api/deployments/${encodeURIComponent(id)}`,
      DeploymentJobSchema,
      { signal },
    ),

  startDeployment: (target: string, release: string, signal?: AbortSignal) =>
    requestData<DeploymentJob>("/api/deployments", DeploymentJobSchema, {
      signal,
      method: "POST",
      action: "deploy",
      body: { target, release },
    }),

  reconcile: (target: string, job: string, signal?: AbortSignal) =>
    requestData<DeploymentJob>(
      `/api/deployments/targets/${encodeURIComponent(target)}/reconcile`,
      DeploymentJobSchema,
      {
        signal,
        method: "POST",
        action: "reconcile",
        body: { job },
      },
    ),
};

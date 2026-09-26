import type {
  DeploymentJob,
  DeploymentStats,
  DeploymentTarget,
  ReleaseArtifact,
  TargetCheck,
  TargetState,
} from '@/domain/deployments';
import { requestData } from '@/integration/common';

/** Deployment operations; every write carries its explicit action header. */
export const deploymentsApi = {
  listHistory: (signal?: AbortSignal) => requestData<DeploymentJob[]>('/api/deployments', { signal }),

  listTargets: (signal?: AbortSignal) => requestData<DeploymentTarget[]>('/api/deployments/targets', { signal }),

  listReleases: (signal?: AbortSignal) => requestData<ReleaseArtifact[]>('/api/deployments/releases', { signal }),

  getStats: (days: number, signal?: AbortSignal) =>
    requestData<DeploymentStats>(`/api/deployments/stats?days=${days}`, { signal }),

  getTargetState: (target: string, signal?: AbortSignal) =>
    requestData<TargetState>(`/api/deployments/targets/${encodeURIComponent(target)}/state`, { signal }),

  checkTarget: (target: string, release?: string) =>
    requestData<TargetCheck>(
      `/api/deployments/targets/${encodeURIComponent(target)}/check` +
        (release ? `?release=${encodeURIComponent(release)}` : ''),
    ),

  getOutcome: (id: string, signal?: AbortSignal) =>
    requestData<DeploymentJob>(`/api/deployments/${encodeURIComponent(id)}`, { signal }),

  startDeployment: (target: string, release: string) =>
    requestData<DeploymentJob>('/api/deployments', {
      method: 'POST',
      action: 'deploy',
      body: JSON.stringify({ target, release }),
    }),

  reconcile: (target: string, job: string) =>
    requestData<DeploymentJob>(`/api/deployments/targets/${encodeURIComponent(target)}/reconcile`, {
      method: 'POST',
      action: 'reconcile',
      body: JSON.stringify({ job }),
    }),
};

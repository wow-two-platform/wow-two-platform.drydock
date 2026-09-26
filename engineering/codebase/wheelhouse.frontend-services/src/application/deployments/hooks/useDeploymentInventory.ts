import { useAppQuery } from '@wow-two-beta/ui/query';
import { deploymentsApi } from '@/integration/deployments';
import { DeploymentKeys } from '../DeploymentKeys';

/** Recent deployments with their last observed outcome. */
export function useDeploymentHistory() {
  return useAppQuery({ key: DeploymentKeys.history, queryFn: ({ signal }) => deploymentsApi.listHistory(signal) });
}

/** Deployment outcomes, rollout time and recovery time over the last `days` UTC days. */
export function useDeploymentStats(days = 30) {
  return useAppQuery({ key: DeploymentKeys.stats(days), queryFn: ({ signal }) => deploymentsApi.getStats(days, signal) });
}

/** The code-owned deployment targets. */
export function useDeploymentTargets() {
  return useAppQuery({ key: DeploymentKeys.targets, queryFn: ({ signal }) => deploymentsApi.listTargets(signal) });
}

/** Published release bundles from approved sources. */
export function useReleaseArtifacts() {
  return useAppQuery({ key: DeploymentKeys.releases, queryFn: ({ signal }) => deploymentsApi.listReleases(signal) });
}

/** What one target runs and whether it accepts a deployment. */
export function useTargetState(target: string) {
  return useAppQuery({
    key: DeploymentKeys.state(target),
    queryFn: ({ signal }) => deploymentsApi.getTargetState(target, signal),
    meta: { suppressGlobalError: true },
  });
}

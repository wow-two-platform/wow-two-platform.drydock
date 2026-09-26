import { useAppMutation } from '@wow-two-beta/ui/query';
import { deploymentsApi } from '@/integration/deployments';
import { DeploymentKeys } from '../DeploymentKeys';

/** A read-only readiness check, run on demand because it reaches the target. */
export function useTargetCheck() {
  return useAppMutation({
    mutationFn: ({ target, release }: { target: string; release?: string }) => deploymentsApi.checkTarget(target, release),
    meta: { suppressGlobalError: true },
  });
}

/** Submits a release to a target; the outcome is followed separately. */
export function useStartDeployment() {
  return useAppMutation({
    mutationFn: ({ target, release }: { target: string; release: string }) =>
      deploymentsApi.startDeployment(target, release),
    invalidates: ({ target }) => [DeploymentKeys.history, DeploymentKeys.state(target)],
    meta: { suppressGlobalError: true },
  });
}

/** Records the operator's reconciliation of a locked target. */
export function useReconcileTarget() {
  return useAppMutation({
    mutationFn: ({ target, job }: { target: string; job: string }) => deploymentsApi.reconcile(target, job),
    invalidates: ({ target }) => [DeploymentKeys.state(target), DeploymentKeys.history],
    meta: { suppressGlobalError: true },
  });
}

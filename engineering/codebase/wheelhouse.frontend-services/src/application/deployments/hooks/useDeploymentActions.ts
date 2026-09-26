import { useAppMutation } from "@/bootstrap/query";
import { TopologyKeys } from "@/application/topology";
import { deploymentsApi } from "@/integration/deployments";

import { DeploymentKeys } from "../DeploymentKeys";

/** A read-only readiness check, run on demand because it reaches the target. */
export function useTargetCheck() {
  return useAppMutation({
    mutationFn: (
      { target, release }: { target: string; release?: string },
      { signal },
    ) => deploymentsApi.checkTarget(target, release, signal),
    meta: { suppressGlobalError: true },
  });
}

/** Submits a release to a target; the outcome is followed separately. */
export function useStartDeployment() {
  return useAppMutation({
    mutationFn: (
      { target, release }: { target: string; release: string },
      { signal },
    ) => deploymentsApi.startDeployment(target, release, signal),
    invalidates: ({ target }) => [
      DeploymentKeys.history,
      DeploymentKeys.state(target),
    ],
    meta: { suppressGlobalError: true },
  });
}

/** Records the operator's reconciliation of a locked target. */
export function useReconcileTarget() {
  return useAppMutation({
    mutationFn: (
      { target, job }: { target: string; job: string },
      { signal },
    ) => deploymentsApi.reconcile(target, job, signal),
    invalidates: ({ target }) => [
      DeploymentKeys.state(target),
      DeploymentKeys.history,
      TopologyKeys.target(target),
    ],
    meta: { suppressGlobalError: true },
  });
}

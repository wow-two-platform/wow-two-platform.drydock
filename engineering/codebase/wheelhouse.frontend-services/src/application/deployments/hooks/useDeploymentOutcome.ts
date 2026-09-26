import { useEffect } from 'react';
import { useAppQuery, useQueryCache } from '@wow-two-beta/ui/query';
import { DeploymentExtensions } from '@/domain/deployments';
import { deploymentsApi } from '@/integration/deployments';
import { FleetKeys } from '@/application/fleet';
import { DeploymentKeys } from '../DeploymentKeys';

const POLL_MS = 3000;

/** Follows one deployment until its target records a final outcome, then refreshes that target. */
export function useDeploymentOutcome(id: string | null) {
  const cache = useQueryCache();
  const outcome = useAppQuery({
    key: DeploymentKeys.outcome(id ?? ''),
    queryFn: ({ signal }) => deploymentsApi.getOutcome(id ?? '', signal),
    enabled: id !== null,
    meta: { suppressGlobalError: true },
  });
  const pending = outcome.data !== undefined && DeploymentExtensions.isPending(outcome.data.status);
  const status = outcome.data?.status;
  const target = outcome.data?.targetId ?? null;
  const { refetch } = outcome;

  // useAppQuery exposes no refetch interval, so polling stays inside this one hook.
  useEffect(() => {
    if (!pending) return;
    const timer = window.setInterval(() => void refetch(), POLL_MS);
    return () => window.clearInterval(timer);
  }, [pending, refetch]);

  // ---- A final outcome changes the target's state and the history ----
  useEffect(() => {
    if (!status || DeploymentExtensions.isPending(status)) return;
    void cache.invalidate(DeploymentKeys.history);
    void cache.invalidate(DeploymentKeys.statsAll);
    void cache.invalidate(FleetKeys.vitals);
    if (target) void cache.invalidate(DeploymentKeys.state(target));
  }, [status, target, cache]);

  return { ...outcome, pending };
}

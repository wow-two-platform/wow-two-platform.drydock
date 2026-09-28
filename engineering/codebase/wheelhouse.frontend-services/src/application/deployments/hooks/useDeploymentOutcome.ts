import {
  computed,
  onScopeDispose,
  toValue,
  watch,
  type MaybeRefOrGetter,
} from "vue";

import { ServerKeys } from "@/application/servers";
import { TopologyKeys } from "@/application/topology";
import { useAppQuery, useInvalidate } from "@/bootstrap/query";
import { DeploymentExtensions } from "@/domain/deployments";
import { deploymentsApi } from "@/integration/deployments";

import { DeploymentKeys } from "../DeploymentKeys";

const PollMilliseconds = 3000;

/** Follows a reactive submission ID until its target records a final outcome. */
export function useDeploymentOutcome(id: MaybeRefOrGetter<string | null>) {
  const invalidate = useInvalidate();
  const outcome = useAppQuery({
    key: () => DeploymentKeys.outcome(toValue(id) ?? ""),
    queryFn: ({ signal }) =>
      deploymentsApi.getOutcome(toValue(id) ?? "", signal),
    enabled: () => Boolean(toValue(id)),
    meta: { suppressGlobalError: true },
  });
  const pending = computed(
    () =>
      outcome.data.value !== undefined &&
      DeploymentExtensions.isPending(outcome.data.value.status),
  );
  let timer: ReturnType<typeof setInterval> | undefined;

  watch(
    pending,
    (active) => {
      if (timer !== undefined) clearInterval(timer);
      timer = active
        ? setInterval(() => void outcome.refetch(), PollMilliseconds)
        : undefined;
    },
    { immediate: true },
  );

  watch(
    () =>
      [
        outcome.data.value?.id,
        outcome.data.value?.status,
        outcome.data.value?.targetId,
      ] as const,
    ([job, status, target]) => {
      if (!job || !status || DeploymentExtensions.isPending(status)) return;
      void invalidate(DeploymentKeys.history);
      void invalidate(DeploymentKeys.statsAll);
      void invalidate(ServerKeys.vitals);
      if (target) {
        void invalidate(DeploymentKeys.state(target));
        void invalidate(TopologyKeys.target(target));
      }
    },
  );

  onScopeDispose(() => {
    if (timer !== undefined) clearInterval(timer);
  });
  return { ...outcome, pending };
}

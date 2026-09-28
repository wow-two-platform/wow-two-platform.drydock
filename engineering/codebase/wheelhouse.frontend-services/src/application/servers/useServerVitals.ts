import { onMounted, onScopeDispose } from "vue";
import { queryClient, useAppQuery } from "@/bootstrap/query";
import { serversApi } from "@/integration/servers";
import { ServerKeys } from "./ServerKeys";

const REFRESH_MS = 60_000;
const readers = new Set<() => unknown>();
let timer: ReturnType<typeof setInterval> | undefined;

/** Shares one visible-page snapshot poll across all mounted server panels. */
export function useServerVitals() {
  const vitals = useAppQuery({
    key: ServerKeys.vitals,
    queryFn: ({ signal }) => serversApi.getVitals(signal),
    meta: { suppressGlobalError: true },
  });
  onMounted(() => {
    readers.add(vitals.refetch);
    timer ??= setInterval(() => {
      // Keep a slow SSH snapshot alive: bare refetch cancels an existing read.
      if (
        document.visibilityState === "visible" &&
        queryClient.isFetching({ queryKey: ServerKeys.vitals }) === 0
      )
        void readers.values().next().value?.();
    }, REFRESH_MS);
  });
  onScopeDispose(() => {
    readers.delete(vitals.refetch);
    if (readers.size === 0 && timer !== undefined) {
      clearInterval(timer);
      timer = undefined;
    }
  });
  return vitals;
}

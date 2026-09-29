import { toValue, type MaybeRefOrGetter } from "vue";
import { useAppQuery } from "@/bootstrap/query";
import { serversApi } from "@/integration/servers";
import { ServerKeys } from "./ServerKeys";

export { ServerKeys } from "./ServerKeys";
export { useServerVitals } from "./useServerVitals";

/** The code-owned servers. */
export function useServers() {
  return useAppQuery({
    key: ServerKeys.servers,
    queryFn: ({ signal }) => serversApi.listServers(signal),
  });
}

/** Every target's stored readings of the last `hours`, for trend lines. */
export function useVitalsHistory(hours: MaybeRefOrGetter<number>) {
  return useAppQuery({
    key: () => ServerKeys.history(toValue(hours)),
    queryFn: ({ signal }) => serversApi.getVitalsHistory(toValue(hours), signal),
    meta: { suppressGlobalError: true },
  });
}

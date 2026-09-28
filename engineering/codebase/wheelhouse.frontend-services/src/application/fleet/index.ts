import { toValue, type MaybeRefOrGetter } from "vue";
import { useAppQuery } from "@/bootstrap/query";
import { fleetApi } from "@/integration/fleet";
import { FleetKeys } from "./FleetKeys";

export { FleetKeys } from "./FleetKeys";
export { useFleetVitals } from "./useFleetVitals";

/** The code-owned fleet hosts. */
export function useServers() {
  return useAppQuery({
    key: FleetKeys.servers,
    queryFn: ({ signal }) => fleetApi.listServers(signal),
  });
}

/** Every target's stored readings of the last `hours`, for trend lines. */
export function useVitalsHistory(hours: MaybeRefOrGetter<number>) {
  return useAppQuery({
    key: () => FleetKeys.history(toValue(hours)),
    queryFn: ({ signal }) => fleetApi.getVitalsHistory(toValue(hours), signal),
    meta: { suppressGlobalError: true },
  });
}

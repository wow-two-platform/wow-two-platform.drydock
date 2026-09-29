import type { ServerVitals, Server, VitalsSample } from "@/domain/servers";
import { requestData } from "@/integration/common";
import { ServerVitalsSchema, ServerSchema, VitalsSampleSchema } from "./schemas";

/** Read-only server catalog and snapshots. */
export const serversApi = {
  listServers: (signal?: AbortSignal) =>
    requestData<Server[]>("/api/servers", ServerSchema.array(), { signal }),
  getVitals: (signal?: AbortSignal) =>
    requestData<ServerVitals>("/api/deployments/vitals", ServerVitalsSchema, {
      signal,
    }),
  /** Every target's stored readings of the last `hours` (1-720), oldest first. */
  getVitalsHistory: (hours: number, signal?: AbortSignal) =>
    requestData<VitalsSample[]>(
      `/api/deployments/vitals/history?hours=${hours}`,
      VitalsSampleSchema.array(),
      { signal },
    ),
};

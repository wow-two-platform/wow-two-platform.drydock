import type { FleetVitals, Server, VitalsSample } from "@/domain/fleet";
import { requestData } from "@/integration/common";
import { FleetVitalsSchema, ServerSchema, VitalsSampleSchema } from "./schemas";

/** Read-only fleet catalog and snapshots. */
export const fleetApi = {
  listServers: (signal?: AbortSignal) =>
    requestData<Server[]>("/api/servers", ServerSchema.array(), { signal }),
  getVitals: (signal?: AbortSignal) =>
    requestData<FleetVitals>("/api/deployments/vitals", FleetVitalsSchema, {
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

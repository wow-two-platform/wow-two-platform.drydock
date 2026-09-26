import type { FleetVitals, Server } from "@/domain/fleet";
import { requestData } from "@/integration/common";
import { FleetVitalsSchema, ServerSchema } from "./schemas";

/** Read-only fleet catalog and snapshots. */
export const fleetApi = {
  listServers: (signal?: AbortSignal) =>
    requestData<Server[]>("/api/servers", ServerSchema.array(), { signal }),
  getVitals: (signal?: AbortSignal) =>
    requestData<FleetVitals>("/api/deployments/vitals", FleetVitalsSchema, {
      signal,
    }),
};

import type { FleetVitals, Server } from '@/domain/fleet';
import { requestData } from '@/integration/common';

/** The read-only, code-owned fleet. */
export const fleetApi = {
  listServers: (signal?: AbortSignal) => requestData<Server[]>('/api/servers', { signal }),

  /** Reads every target's host and containers over SSH; nothing on the hosts changes. */
  getVitals: (signal?: AbortSignal) => requestData<FleetVitals>('/api/deployments/vitals', { signal }),
};

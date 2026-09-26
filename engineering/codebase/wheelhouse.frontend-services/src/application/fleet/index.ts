import { useAppQuery } from '@wow-two-beta/ui/query';
import { fleetApi } from '@/integration/fleet';
import { FleetKeys } from './FleetKeys';

export { FleetKeys } from './FleetKeys';
export { useFleetVitals } from './useFleetVitals';

/** The code-owned fleet hosts. */
export function useServers() {
  return useAppQuery({ key: FleetKeys.servers, queryFn: ({ signal }) => fleetApi.listServers(signal) });
}

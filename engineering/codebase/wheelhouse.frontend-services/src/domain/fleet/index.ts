/** Providers implemented by the code-owned fleet catalog; adding one requires code. */
export const VpsProvider = {
  Hetzner: 'Hetzner',
  Local: 'Local',
} as const;
export type VpsProvider = (typeof VpsProvider)[keyof typeof VpsProvider];

/** A host defined in the fleet catalog. */
export interface Server {
  id: string;
  name: string;
  provider: VpsProvider;
  host: string;
  region: string;
  sshUser: string;
}

export type { ContainerVitals, DiskUsage, FleetVitals, HostVitals, TargetVitals } from './models/FleetVitals';
export { FleetExtensions, RESTART_WARNING, USAGE_THRESHOLDS, type ContainerTone } from './FleetExtensions';

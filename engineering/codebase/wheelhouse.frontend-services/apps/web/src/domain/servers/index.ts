/** Providers implemented by the code-owned server catalog; adding one requires code. */
export const VpsProvider = {
  Hetzner: 'Hetzner',
  Local: 'Local',
} as const;
export type VpsProvider = (typeof VpsProvider)[keyof typeof VpsProvider];

/** A server defined in the code-owned catalog. */
export interface Server {
  id: string;
  name: string;
  provider: VpsProvider;
  host: string;
  region: string;
  sshUser: string;
}

export type { ContainerVitals, DiskUsage, ServerVitals, HostVitals, TargetVitals } from './models/ServerVitals';
export { TrendRanges, hostTrend, type TrendPoint, type TrendRange, type VitalsSample } from './models/VitalsSample';
export { ServerExtensions, RESTART_WARNING, USAGE_THRESHOLDS, type ContainerTone } from './ServerExtensions';

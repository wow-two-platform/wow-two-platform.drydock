import type { TargetCondition } from '@/domain/deployments';

/** One filesystem's capacity on a host. */
export interface DiskUsage {
  path: string;
  totalBytes: number;
  freeBytes: number;
}

/** A host's load, memory, disks and uptime; a field is null where the host hides it. */
export interface HostVitals {
  cpus: number | null;
  load: [number, number, number] | null;
  memoryTotalBytes: number | null;
  memoryAvailableBytes: number | null;
  uptimeSeconds: number | null;
  disks: DiskUsage[];
}

/** One container's state and resource use; never its environment or logs. */
export interface ContainerVitals {
  service: string;
  state: string | null;
  health: string | null;
  restarts: number;
  startedAt: string | null;
  exitCode: number | null;
  cpuPercent: number | null;
  memoryBytes: number | null;
  memoryLimitBytes: number | null;
}

/** What one target's host and containers looked like; `ok` is false when the target could not be read. */
export interface TargetVitals {
  targetId: string;
  serverId: string;
  ok: boolean;
  reason?: string | null;
  project?: string;
  host?: HostVitals | null;
  containers?: ContainerVitals[] | null;
  problems?: string[];
  condition?: TargetCondition;
  release?: string | null;
}

/** Every target's vitals from one collection pass. */
export interface FleetVitals {
  collectedAt: string;
  targets: TargetVitals[];
}

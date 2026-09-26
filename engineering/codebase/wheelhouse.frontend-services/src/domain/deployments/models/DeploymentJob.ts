import type { JobStatus } from '../enums/JobStatus';

/** One submitted deployment with its last observed outcome; reasons never carry values. */
export interface DeploymentJob {
  id: string;
  targetId?: string | null;
  bundleId?: string | null;
  release?: string | null;
  actor?: string | null;
  submittedAt?: string;
  status: JobStatus;
  reason?: string | null;
  failure?: string | null;
  startedAt?: string;
  completedAt?: string;
  mutationStarted?: boolean;
}

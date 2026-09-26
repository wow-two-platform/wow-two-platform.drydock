import type { JobStatus } from '../enums/JobStatus';
import type { TargetCondition } from '../enums/TargetCondition';

/** A rollout record held on the target. */
export interface RolloutRecord {
  id: string;
  release?: string;
  status?: JobStatus;
  actor?: string;
  startedAt?: string;
  completedAt?: string;
  reason?: string;
  mutationStarted?: boolean;
}

/** What a target runs and whether it accepts a deployment. */
export interface TargetState {
  targetId: string;
  project: string;
  condition: TargetCondition;
  current: RolloutRecord | null;
  active: RolloutRecord | null;
}

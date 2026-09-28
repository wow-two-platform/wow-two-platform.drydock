import type { JobStatus } from '../enums/JobStatus';
import type { TargetCondition } from '../enums/TargetCondition';
import type { DeploymentStep } from './DeploymentStep';
import type { PublishedSite } from './PublishedSite';
import type { ReleaseKind } from './ReleaseArtifact';
import type { ServiceVersion } from './ServiceVersion';

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
  kind?: ReleaseKind;
  branch?: string;
  sourceCommit?: string;
  /** Each service's version in this release. */
  versions?: Record<string, ServiceVersion>;
  /** The sites a verified release answers on; recorded once the rollout succeeds. */
  sites?: PublishedSite[];
  steps?: DeploymentStep[];
  warnings?: string[];
}

/** What a target runs and whether it accepts a deployment. */
export interface TargetState {
  targetId: string;
  project: string;
  condition: TargetCondition;
  current: RolloutRecord | null;
  active: RolloutRecord | null;
}

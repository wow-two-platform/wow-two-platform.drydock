import type { JobStatus } from '../enums/JobStatus';

/** Finished deployments on one UTC day, by outcome. */
export interface DailyDeploys {
  date: string;
  succeeded: number;
  failed: number;
  refused: number;
}

/** One target's deployments in the window. */
export interface TargetDeployStats {
  targetId: string;
  deploys: number;
  succeeded: number;
  failed: number;
  lastStatus: JobStatus;
  lastRelease?: string | null;
  lastDeployAt: string;
  /** When the target's unrecovered failure began; null once a later deploy succeeded. */
  failingSince: string | null;
}

/** Outcome counts, success rate, and rollout and recovery times of one window of UTC days. */
export interface DeploymentMeasures {
  /** The window's first UTC day. */
  since: string;
  deploys: number;
  succeeded: number;
  failed: number;
  refused: number;
  pending: number;
  /** Succeeded share of finished rollouts; refusals changed nothing and are excluded. */
  successRate: number | null;
  medianRolloutSeconds: number | null;
  /** Median time from a failed rollout to the next success on the same target. */
  medianRecoverySeconds: number | null;
}

/** Deployment outcomes, rollout time and recovery time over a window of UTC days. */
export interface DeploymentStats extends DeploymentMeasures {
  windowDays: number;
  /** The same measures for the window before, for trends; null when it holds no records. */
  previous: DeploymentMeasures | null;
  daily: DailyDeploys[];
  targets: TargetDeployStats[];
}

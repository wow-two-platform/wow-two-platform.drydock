import type { StepStatus } from '../enums/StepStatus';

/** One step of a rollout; details are operator-safe text and never carry setting values. */
export interface DeploymentStep {
  name: string;
  status: StepStatus;
  startedAt?: string;
  completedAt?: string;
  detail?: string;
}

import { Badge } from '@wow-two-beta/ui/presentation/display';
import { StatusIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { DeploymentExtensions, type JobStatus, type TargetCondition } from '@/domain/deployments';

/** A deployment status as a colored badge. */
export function JobStatusBadge(props: { status: JobStatus }) {
  return <Badge variant={DeploymentExtensions.statusVariant(props.status)}>{DeploymentExtensions.label(props.status)}</Badge>;
}

/** A deployment status as a dot for dense tables, the reason beneath it; a pending status pulses. */
export function JobStatusIndicator(props: { status: JobStatus; reason?: string | null | undefined }) {
  return (
    <StatusIndicator tone={DeploymentExtensions.statusTone(props.status)} label={DeploymentExtensions.label(props.status)}
      hasPulse={DeploymentExtensions.isPending(props.status)} {...(props.reason ? { description: props.reason } : {})} />
  );
}

/** A target condition as a colored badge. */
export function ConditionBadge(props: { condition: TargetCondition }) {
  return (
    <Badge variant={DeploymentExtensions.conditionVariant(props.condition)}>{DeploymentExtensions.label(props.condition)}</Badge>
  );
}

import { useState } from 'react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Badge, Text } from '@wow-two-beta/ui/presentation/display';
import { useDeploymentTargets, useReleaseArtifacts, useTargetState } from '@/application/deployments';
import { DeploymentExtensions, TargetCondition, type DeploymentTarget, type ReleaseArtifact } from '@/domain/deployments';
import { LoadState, Panel } from '@/presentation/common/components';
import { Skeleton } from '@/presentation/common/skeleton';
import { ConditionBadge } from './JobStatusBadge';
import { ReconcileModal } from './ReconcileModal';

/** What each environment runs, and a way to reconcile a locked one. */
export function TargetsPanel() {
  const targets = useDeploymentTargets();
  const releases = useReleaseArtifacts();
  return (
    <Panel title="Targets" description="The release each environment runs; locked targets need reconciliation.">
      <LoadState loading={targets.loading} error={targets.error} empty={!targets.data?.length}
        emptyTitle="No deployment targets" emptyDescription="Targets are defined in reviewed code (fleet.py)."
        onRetry={() => void targets.refetch()}
        skeleton={
          <Skeleton.Group loading label="Loading targets">
            <ul className="divide-y divide-border">
              {[1, 2].map((index) => (
                <li key={index} className="flex flex-wrap items-center justify-between gap-3 py-3">
                  <TargetIdentity id="product-environment" detail="product · environment · 0.0.0.0" />
                  <TargetStatePlaceholder />
                </li>
              ))}
            </ul>
          </Skeleton.Group>
        }>
        <ul className="divide-y divide-border">
          {(targets.data ?? []).map((target) => <TargetRow key={target.id} target={target} releases={releases.data ?? []} />)}
        </ul>
      </LoadState>
    </Panel>
  );
}

// ---- Row ----

function TargetIdentity(props: { id: string; detail: string }) {
  return (
    <div className="flex flex-col items-start">
      <Skeleton.Slot className="font-mono text-sm">{props.id}</Skeleton.Slot>
      <Skeleton.Slot className="text-xs text-muted-foreground">{props.detail}</Skeleton.Slot>
    </div>
  );
}

// The shape of a target's condition and release while its state is read.
function TargetStatePlaceholder() {
  return (
    <span className="flex items-center gap-3">
      <Skeleton.Slot><ConditionBadge condition={TargetCondition.Ready} /></Skeleton.Slot>
      <Skeleton.Slot className="text-sm">verified release-1</Skeleton.Slot>
    </span>
  );
}

function TargetRow(props: { target: DeploymentTarget; releases: ReleaseArtifact[] }) {
  const state = useTargetState(props.target.id);
  const [reconciling, setReconciling] = useState(false);
  const data = state.data;
  const drift = DeploymentExtensions.releaseDrift(data?.current?.release, props.releases, props.target.product);
  return (
    <li className="flex flex-wrap items-center justify-between gap-3 py-3">
      <TargetIdentity id={props.target.id}
        detail={`${props.target.product} · ${props.target.environment} · ${props.target.host}`} />
      <div className="flex flex-wrap items-center gap-3 text-sm">
        {state.loading ? (
          <Skeleton.Group loading label="Reading target"><TargetStatePlaceholder /></Skeleton.Group>
        ) : state.error ? (
          <>
            <Text size="sm" color="danger">{state.error.message}</Text>
            <Button variant="ghost" tone="neutral" size="sm" onClick={() => void state.refetch()}>Retry</Button>
          </>
        ) : data && (
          <>
            <ConditionBadge condition={data.condition} />
            <Text size="sm" color="muted">{data.current?.release ? `verified ${data.current.release}` : 'no verified release'}</Text>
            {drift.behind === 0 && <Badge variant="success">latest</Badge>}
            {drift.behind !== null && drift.behind > 0 && (
              <Badge variant="warning" title={`Latest published: ${drift.latest?.release ?? ''}`}>
                {drift.behind} {drift.behind === 1 ? 'release' : 'releases'} behind
              </Badge>
            )}
            {drift.behind === null && data.current?.release && drift.latest && (
              <Text size="xs" color="muted">latest published {drift.latest.release}</Text>
            )}
            {data.condition === TargetCondition.NeedsReconciliation && data.active && (
              <>
                <Button variant="soft" tone="danger" size="sm" onClick={() => setReconciling(true)}>Reconcile</Button>
                <ReconcileModal target={props.target.id} active={data.active} open={reconciling} onOpenChange={setReconciling} />
              </>
            )}
          </>
        )}
      </div>
    </li>
  );
}

import { useEffect, useState } from 'react';
import { ArrowLeft, Rocket, ShieldCheck } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Text } from '@wow-two-beta/ui/presentation/display';
import { Alert, Spinner } from '@wow-two-beta/ui/presentation/feedback';
import { Field, Select } from '@wow-two-beta/ui/presentation/forms';
import { Modal } from '@wow-two-beta/ui/presentation/overlays';
import {
  useDeploymentOutcome,
  useDeploymentTargets,
  useReleaseArtifacts,
  useStartDeployment,
  useTargetCheck,
} from '@/application/deployments';
import { JobStatus, type DeploymentTarget, type ReleaseArtifact } from '@/domain/deployments';
import { CheckResultList } from './CheckResultList';
import { JobStatusBadge } from './JobStatusBadge';

const targetLabel = (item?: DeploymentTarget) => item && `${item.product} · ${item.environment} · ${item.host}`;
const releaseLabel = (item?: ReleaseArtifact) => item && item.release + (item.prerelease ? ' (prerelease)' : '');

/** A target and release to preselect, such as a redeploy chosen from the history. */
export interface DeploySelection {
  target: string;
  release: string;
}

/** Props for {@link DeployModal}. */
export interface DeployModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Where to start; remount the modal (a new `key`) for each opening so it starts fresh. */
  selection?: DeploySelection | null;
}

// Choose a target and release, confirm the replacement, then follow the outcome.
type Step = 'choose' | 'confirm' | 'follow';

/** Checks a target and deploys a published release to it, then follows the outcome until it settles. */
export function DeployModal(props: DeployModalProps) {
  const targets = useDeploymentTargets();
  const releases = useReleaseArtifacts();
  const check = useTargetCheck();
  const start = useStartDeployment();
  const [step, setStep] = useState<Step>('choose');
  const [target, setTarget] = useState(props.selection?.target ?? '');
  const [release, setRelease] = useState(props.selection?.release ?? '');
  const [jobId, setJobId] = useState<string | null>(null);
  const outcome = useDeploymentOutcome(jobId);
  const { refetch: refetchReleases } = releases;

  const selectedTarget = targets.data?.find((item) => item.id === target);
  const available = (releases.data ?? []).filter((item) => item.product === selectedTarget?.product);
  const selectedRelease = available.find((item) => item.id === release);

  // ---- Each opening lists the newest published releases ----
  useEffect(() => {
    if (props.open) void refetchReleases();
  }, [props.open, refetchReleases]);

  function selectTarget(value: string) {
    setTarget(value);
    setRelease('');
    check.reset();
  }

  async function deploy() {
    const job = await start.mutateAsync({ target, release }).catch(() => null);
    if (job) {
      setJobId(job.id);
      setStep('follow');
    }
  }

  return (
    <Modal open={props.open} onOpenChange={props.onOpenChange}>
      <Modal.Content className="w-full max-w-xl">
        {step === 'choose' && (
          <>
            <Modal.Header>
              <Modal.Title>Deploy a release</Modal.Title>
              <Modal.Description>Pick an environment and a published release; check readiness before you deploy.</Modal.Description>
            </Modal.Header>
            <Modal.Body className="flex flex-col gap-4">
              {targets.loading || releases.loading ? (
                <div className="flex justify-center py-6"><Spinner label="Loading targets and releases" /></div>
              ) : targets.error ?? releases.error ? (
                <Alert severity="danger" title="Couldn't load" description={(targets.error ?? releases.error)?.message} />
              ) : !targets.data?.length ? (
                <Alert severity="info" title="No deployment targets" description="Targets are defined in reviewed code (fleet.py)." />
              ) : (
                <>
                  <Field label="Environment">
                    <Select<string> value={target || null} onValueChange={(option) => selectTarget(option?.value ?? '')}
                      getOptionLabel={(key) => targetLabel(targets.data?.find((item) => item.id === key)) ?? key}>
                      <Select.Trigger aria-label="Environment"><Select.Value placeholder="Select an environment" /></Select.Trigger>
                      <Select.Content>
                        {(targets.data ?? []).map((item) => (
                          <Select.Item key={item.id} itemKey={item.id} label={targetLabel(item)} />
                        ))}
                      </Select.Content>
                    </Select>
                  </Field>
                  <Field label="Release" helper={target && !available.length ? 'No published release for this product yet.' : undefined}>
                    <Select<string> value={release || null} onValueChange={(option) => setRelease(option?.value ?? '')}
                      isDisabled={!available.length} getOptionLabel={(key) => releaseLabel(available.find((item) => item.id === key)) ?? key}>
                      <Select.Trigger aria-label="Release"><Select.Value placeholder="Select a published release" /></Select.Trigger>
                      <Select.Content>
                        {available.map((item) => (
                          <Select.Item key={item.id} itemKey={item.id} label={releaseLabel(item)} />
                        ))}
                      </Select.Content>
                    </Select>
                  </Field>
                  {check.error && <Alert severity="danger" title="Check unavailable" description={check.error.message} />}
                  {check.data && (
                    <div className="flex flex-col gap-2 rounded-lg border border-border p-4">
                      <Text size="sm" weight="medium">
                        {check.data.ok ? 'Ready to deploy' : 'Not ready — resolve the failed checks first'}
                      </Text>
                      <CheckResultList check={check.data} />
                    </div>
                  )}
                </>
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="outline" tone="neutral" leadingSlot={<ShieldCheck size={16} />} isLoading={check.loading}
                isDisabled={!target} onClick={() => check.mutate({ target, ...(release ? { release } : {}) })}>
                Check readiness
              </Button>
              <Button variant="solid" tone="primary" isDisabled={!selectedRelease} onClick={() => setStep('confirm')}>
                Continue
              </Button>
            </Modal.Footer>
          </>
        )}

        {step === 'confirm' && (
          <>
            <Modal.Header>
              <Modal.Title>Deploy {selectedRelease?.release} to {target}?</Modal.Title>
              <Modal.Description>
                The target pulls every image first, then replaces its containers and waits for health and smoke checks.
                Replacement has a short restart window.
              </Modal.Description>
            </Modal.Header>
            <Modal.Body className="flex flex-col gap-3">
              <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 rounded-lg border border-border p-4 text-sm">
                <dt className="text-muted-foreground">Environment</dt>
                <dd>{targetLabel(selectedTarget)}</dd>
                <dt className="text-muted-foreground">Release</dt>
                <dd className="font-mono">{releaseLabel(selectedRelease)}</dd>
                <dt className="text-muted-foreground">Readiness</dt>
                <dd>{!check.data ? 'Not checked' : check.data.ok ? 'Ready' : 'Failed checks — the runner refuses the deploy'}</dd>
              </dl>
              {start.error && <Alert severity="danger" title="Deployment refused" description={start.error.message} />}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="outline" tone="neutral" leadingSlot={<ArrowLeft size={16} />} isDisabled={start.loading}
                onClick={() => setStep('choose')}>
                Back
              </Button>
              <Button variant="solid" tone="primary" leadingSlot={<Rocket size={16} />} isLoading={start.loading}
                onClick={() => void deploy()}>
                Deploy
              </Button>
            </Modal.Footer>
          </>
        )}

        {step === 'follow' && (
          <>
            <Modal.Header>
              <Modal.Title>{outcome.pending || !outcome.data ? 'Deploying' : 'Deployment settled'} {selectedRelease?.release}</Modal.Title>
              <Modal.Description>Closing this keeps the rollout running; the history records its outcome.</Modal.Description>
            </Modal.Header>
            <Modal.Body className="flex flex-col gap-3">
              <div role="status" className="flex flex-col gap-2 rounded-lg border border-border p-4">
                <div className="flex flex-wrap items-center gap-2">
                  <Text size="sm">Deployment <span className="font-mono">{jobId?.slice(0, 8)}</span></Text>
                  {outcome.data ? <JobStatusBadge status={outcome.data.status} /> : <Spinner size="sm" label="Waiting for the runner" />}
                </div>
                {outcome.data?.reason && <Text size="sm" color="muted">{outcome.data.reason}</Text>}
              </div>
              {outcome.data?.mutationStarted
                && (outcome.data.status === JobStatus.Failed || outcome.data.status === JobStatus.RollbackFailed) && (
                <Alert severity="warning" description="The target stays locked until an operator reconciles it under Targets." />
              )}
              {outcome.error && <Alert severity="warning" title="Outcome unavailable" description={outcome.error.message} />}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="outline" tone="neutral" onClick={() => props.onOpenChange(false)}>Close</Button>
            </Modal.Footer>
          </>
        )}
      </Modal.Content>
    </Modal>
  );
}

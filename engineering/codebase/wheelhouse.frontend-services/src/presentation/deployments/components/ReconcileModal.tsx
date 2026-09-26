import { useState } from 'react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Text } from '@wow-two-beta/ui/presentation/display';
import { Alert } from '@wow-two-beta/ui/presentation/feedback';
import { Field, TextInput } from '@wow-two-beta/ui/presentation/forms';
import { AlertModal } from '@wow-two-beta/ui/presentation/overlays';
import { useReconcileTarget } from '@/application/deployments';
import type { RolloutRecord } from '@/domain/deployments';

/** Props for {@link ReconcileModal}. */
export interface ReconcileModalProps {
  target: string;
  active: RolloutRecord;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** Confirms that the operator inspected a locked target before it accepts deployments again. */
export function ReconcileModal(props: ReconcileModalProps) {
  const reconcile = useReconcileTarget();
  const [typed, setTyped] = useState('');
  const confirmed = typed === props.target;

  function close(open: boolean) {
    if (!open) {
      setTyped('');
      reconcile.reset();
    }
    props.onOpenChange(open);
  }

  async function confirm() {
    const done = await reconcile.mutateAsync({ target: props.target, job: props.active.id }).catch(() => null);
    if (done) close(false);
  }

  return (
    <AlertModal open={props.open} onOpenChange={close}>
      <AlertModal.Content>
        <AlertModal.Header>
          <AlertModal.Title>Reconcile {props.target}?</AlertModal.Title>
          <AlertModal.Description>
            Confirm only after inspecting the target's containers and database schema. Reconciling marks the
            rollout interrupted and clears the automatic-rollback pointer; it changes no containers or data.
          </AlertModal.Description>
        </AlertModal.Header>
        <AlertModal.Body className="flex flex-col gap-3">
          <Text size="sm" color="muted">
            Stopped rollout: {props.active.release ?? props.active.id}
            {props.active.reason ? ` — ${props.active.reason}` : ''}
          </Text>
          <Field label={`Type ${props.target} to confirm`}>
            <TextInput value={typed} onChange={(event) => setTyped(event.target.value)} autoComplete="off" />
          </Field>
          {reconcile.error && <Alert severity="danger" description={reconcile.error.message} />}
        </AlertModal.Body>
        <AlertModal.Footer>
          <Button variant="outline" tone="neutral" onClick={() => close(false)}>Cancel</Button>
          <Button variant="solid" tone="danger" isDisabled={!confirmed} isLoading={reconcile.loading}
            onClick={() => void confirm()}>
            Reconcile
          </Button>
        </AlertModal.Footer>
      </AlertModal.Content>
    </AlertModal>
  );
}

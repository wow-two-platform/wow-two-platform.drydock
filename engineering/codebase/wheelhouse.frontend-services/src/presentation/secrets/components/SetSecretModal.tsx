import { useState, type FormEvent } from 'react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Alert } from '@wow-two-beta/ui/presentation/feedback';
import { Field, PasswordInput, TextInput } from '@wow-two-beta/ui/presentation/forms';
import { Modal } from '@wow-two-beta/ui/presentation/overlays';
import { useSecretChanges } from '@/application/secrets';

/** Props for {@link SetSecretModal}. */
export interface SetSecretModalProps {
  vault: string;
  ns: string;
  /** The key being rotated; omitted when adding a new secret. */
  secretKey?: string | undefined;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** Writes a new secret version; the value goes to the vault and is cleared from the form immediately. */
export function SetSecretModal(props: SetSecretModalProps) {
  const changes = useSecretChanges(props.vault, props.ns);
  const [key, setKey] = useState('');
  const [value, setValue] = useState('');
  const [description, setDescription] = useState('');
  const effectiveKey = props.secretKey ?? key;

  function close(open: boolean) {
    if (!open) {
      setKey('');
      setValue('');
      setDescription('');
      changes.reset();
    }
    props.onOpenChange(open);
  }

  async function save(event: FormEvent) {
    event.preventDefault();
    const saved = await changes.set(effectiveKey, value, description || undefined);
    setValue('');
    if (saved !== null) close(false);
  }

  return (
    <Modal open={props.open} onOpenChange={close}>
      <Modal.Content>
        <form onSubmit={(event) => void save(event)}>
          <Modal.Header>
            <Modal.Title>{props.secretKey ? `Rotate ${props.secretKey}` : 'Add secret'}</Modal.Title>
            <Modal.Description>
              The value goes straight to the vault as a new version. Wheelhouse never shows it again.
            </Modal.Description>
          </Modal.Header>
          <Modal.Body className="flex flex-col gap-3">
            {!props.secretKey && (
              <Field label="Key" helper="e.g. DATABASE_URL or Billing:SecretKey">
                <TextInput value={key} onChange={(event) => setKey(event.target.value)} autoComplete="off" required />
              </Field>
            )}
            <Field label="Value">
              <PasswordInput value={value} onChange={(event) => setValue(event.target.value)} autoComplete="new-password" required />
            </Field>
            <Field label="Description">
              <TextInput value={description} onChange={(event) => setDescription(event.target.value)} autoComplete="off" />
            </Field>
            {changes.error && <Alert severity="danger" description={changes.error.message} />}
          </Modal.Body>
          <Modal.Footer>
            <Button variant="outline" tone="neutral" onClick={() => close(false)}>Cancel</Button>
            <Button type="submit" isLoading={changes.loading} isDisabled={!effectiveKey || !value}>Save version</Button>
          </Modal.Footer>
        </form>
      </Modal.Content>
    </Modal>
  );
}

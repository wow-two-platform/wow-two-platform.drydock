import { useState, type FormEvent } from 'react';
import { Button, CopyButton } from '@wow-two-beta/ui/presentation/actions';
import { Code, Text } from '@wow-two-beta/ui/presentation/display';
import { Alert } from '@wow-two-beta/ui/presentation/feedback';
import { Field, TextInput } from '@wow-two-beta/ui/presentation/forms';
import { Modal } from '@wow-two-beta/ui/presentation/overlays';
import { useTokenChanges } from '@/application/secrets';
import type { MintedToken } from '@/domain/secrets';

/** Props for {@link MintTokenModal}. */
export interface MintTokenModalProps {
  vault: string;
  ns: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** Mints a product token and shows it exactly once; closing the dialog forgets it. */
export function MintTokenModal(props: MintTokenModalProps) {
  const changes = useTokenChanges(props.vault, props.ns);
  const [name, setName] = useState('');
  const [minted, setMinted] = useState<MintedToken | null>(null);

  function close(open: boolean) {
    if (!open) {
      setName('');
      setMinted(null);
      changes.reset();
    }
    props.onOpenChange(open);
  }

  async function mint(event: FormEvent) {
    event.preventDefault();
    setMinted(await changes.mint(name));
  }

  return (
    <Modal open={props.open} onOpenChange={close}>
      <Modal.Content>
        <Modal.Header>
          <Modal.Title>{minted ? 'Copy the token now' : 'Mint product token'}</Modal.Title>
          <Modal.Description>
            {minted
              ? 'This is the only time the token is shown. The vault keeps only its hash.'
              : `The token reads secrets in ${props.ns} only. Mount it into the product; never commit it.`}
          </Modal.Description>
        </Modal.Header>
        {minted ? (
          <>
            <Modal.Body className="flex flex-col gap-3">
              <Code className="break-all">{minted.token}</Code>
              <Text size="sm" color="muted">Token {minted.name} for {minted.namespace}</Text>
            </Modal.Body>
            <Modal.Footer>
              <CopyButton text={minted.token} aria-label="Copy token" variant="outline" tone="neutral" />
              <Button onClick={() => close(false)}>Done</Button>
            </Modal.Footer>
          </>
        ) : (
          <form onSubmit={(event) => void mint(event)}>
            <Modal.Body className="flex flex-col gap-3">
              <Field label="Name" helper="Who uses it, e.g. management">
                <TextInput value={name} onChange={(event) => setName(event.target.value)} autoComplete="off" required />
              </Field>
              {changes.error && <Alert severity="danger" description={changes.error.message} />}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="outline" tone="neutral" onClick={() => close(false)}>Cancel</Button>
              <Button type="submit" isLoading={changes.loading} isDisabled={!name}>Mint token</Button>
            </Modal.Footer>
          </form>
        )}
      </Modal.Content>
    </Modal>
  );
}

import { useState, type FormEvent } from 'react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Alert } from '@wow-two-beta/ui/presentation/feedback';
import { Field, TextInput } from '@wow-two-beta/ui/presentation/forms';
import { Modal } from '@wow-two-beta/ui/presentation/overlays';
import { useNamespaceCreate } from '@/application/secrets';

/** Props for {@link NamespaceModal}. */
export interface NamespaceModalProps {
  vault: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Called with the new namespace's slug once the vault created it. */
  onCreated: (slug: string) => void;
}

/** Creates a vault namespace — one per product environment. */
export function NamespaceModal(props: NamespaceModalProps) {
  const creator = useNamespaceCreate(props.vault);
  const [slug, setSlug] = useState('');
  const [name, setName] = useState('');

  function close(open: boolean) {
    if (!open) {
      setSlug('');
      setName('');
    }
    props.onOpenChange(open);
  }

  async function create(event: FormEvent) {
    event.preventDefault();
    if (await creator.create(slug, name) !== null) {
      props.onCreated(slug);
      close(false);
    }
  }

  return (
    <Modal open={props.open} onOpenChange={close}>
      <Modal.Content className="w-full max-w-md">
        <form onSubmit={(event) => void create(event)}>
          <Modal.Header>
            <Modal.Title>New namespace</Modal.Title>
            <Modal.Description>One per product environment; its secrets and tokens live inside it.</Modal.Description>
          </Modal.Header>
          <Modal.Body className="flex flex-col gap-4">
            <Field label="Slug" helper="Lowercase, e.g. foreverpin-staging">
              <TextInput value={slug} onChange={(event) => setSlug(event.target.value)} autoComplete="off" required />
            </Field>
            <Field label="Name">
              <TextInput value={name} onChange={(event) => setName(event.target.value)} autoComplete="off" required />
            </Field>
            {creator.error && <Alert severity="danger" description={creator.error.message} />}
          </Modal.Body>
          <Modal.Footer>
            <Button type="button" variant="outline" tone="neutral" onClick={() => close(false)}>Cancel</Button>
            <Button type="submit" isLoading={creator.loading} isDisabled={!slug || !name}>Create namespace</Button>
          </Modal.Footer>
        </form>
      </Modal.Content>
    </Modal>
  );
}

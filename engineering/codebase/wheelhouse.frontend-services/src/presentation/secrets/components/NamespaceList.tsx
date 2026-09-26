import { useState, type FormEvent } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Text } from '@wow-two-beta/ui/presentation/display';
import { Alert } from '@wow-two-beta/ui/presentation/feedback';
import { Field, TextInput } from '@wow-two-beta/ui/presentation/forms';
import { useNamespaceCreate, useVaultNamespaces } from '@/application/secrets';
import { LoadState } from '@/presentation/common/components';

/** Props for {@link NamespaceList}. */
export interface NamespaceListProps {
  vault: string;
  selected: string;
  onSelect: (slug: string) => void;
}

/** A vault's namespaces, with a way to add one. */
export function NamespaceList(props: NamespaceListProps) {
  const namespaces = useVaultNamespaces(props.vault);
  const creator = useNamespaceCreate(props.vault);
  const [adding, setAdding] = useState(false);
  const [slug, setSlug] = useState('');
  const [name, setName] = useState('');

  async function create(event: FormEvent) {
    event.preventDefault();
    if (await creator.create(slug, name) !== null) {
      props.onSelect(slug);
      setAdding(false);
      setSlug('');
      setName('');
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <Text size="sm" weight="medium">Namespaces</Text>
        <Button variant="ghost" tone="neutral" size="sm" leadingSlot={<Plus size={14} />} onClick={() => setAdding((value) => !value)}>
          {adding ? 'Close' : 'New'}
        </Button>
      </div>
      {adding && (
        <form className="flex flex-col gap-2 rounded-lg border border-border p-3" onSubmit={(event) => void create(event)}>
          <Field label="Slug" helper="Lowercase, e.g. foreverpin-staging">
            <TextInput value={slug} onChange={(event) => setSlug(event.target.value)} autoComplete="off" required />
          </Field>
          <Field label="Name">
            <TextInput value={name} onChange={(event) => setName(event.target.value)} autoComplete="off" required />
          </Field>
          {creator.error && <Alert severity="danger" description={creator.error.message} />}
          <Button type="submit" size="sm" isLoading={creator.loading} isDisabled={!slug || !name}>Create namespace</Button>
        </form>
      )}
      <LoadState loading={namespaces.loading} error={namespaces.error} empty={!namespaces.data?.length}
        emptyTitle="No namespaces yet" emptyDescription="Create one per product environment." onRetry={() => void namespaces.refetch()}>
        <ul className="flex flex-col gap-1">
          {(namespaces.data ?? []).map((item) => (
            <li key={item.slug}>
              <button type="button" onClick={() => props.onSelect(item.slug)}
                className={`w-full rounded-md px-3 py-2 text-left text-sm transition-colors hover:bg-muted ${
                  item.slug === props.selected ? 'bg-muted font-medium' : ''}`}>
                <span className="block font-mono text-xs">{item.slug}</span>
                <span className="block text-xs text-muted-foreground">{item.name}</span>
              </button>
            </li>
          ))}
        </ul>
      </LoadState>
    </div>
  );
}

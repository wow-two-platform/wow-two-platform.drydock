import { useState } from 'react';
import { Plus } from 'lucide-react';
import { cn } from '@wow-two-beta/ui/foundation/utils';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { useVaultNamespaces } from '@/application/secrets';
import { LoadState } from '@/presentation/common/components';
import { Skeleton } from '@/presentation/common/skeleton';
import { NamespaceModal } from './NamespaceModal';

/** Props for {@link NamespaceList}. */
export interface NamespaceListProps {
  vault: string;
  selected: string;
  onSelect: (slug: string) => void;
}

/** A vault's namespaces as a rail, with a way to add one. */
export function NamespaceList(props: NamespaceListProps) {
  const namespaces = useVaultNamespaces(props.vault);
  const [adding, setAdding] = useState(false);

  return (
    <div className="flex flex-col gap-3">
      <div className="flex h-8 items-center justify-between gap-2">
        <span className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">Namespaces</span>
        <Button variant="ghost" tone="neutral" size="sm" leadingSlot={<Plus size={14} />} onClick={() => setAdding(true)}>
          New
        </Button>
      </div>
      <LoadState loading={namespaces.loading} error={namespaces.error} empty={!namespaces.data?.length}
        emptyTitle="No namespaces yet" emptyDescription="Create one per product environment."
        emptyActions={<Button size="sm" onClick={() => setAdding(true)}>New namespace</Button>}
        onRetry={() => void namespaces.refetch()}
        skeleton={
          <Skeleton.Group loading label="Loading namespaces" className="flex flex-col gap-1">
            {[1, 2].map((index) => <Skeleton key={index} className="h-12 w-full rounded-md" />)}
          </Skeleton.Group>
        }>
        <ul className="flex flex-col gap-1">
          {(namespaces.data ?? []).map((item) => {
            const active = item.slug === props.selected;
            return (
              <li key={item.slug}>
                <button type="button" onClick={() => props.onSelect(item.slug)} aria-current={active || undefined}
                  className={cn(
                    'relative flex h-12 w-full flex-col justify-center rounded-md px-3 text-left transition-colors',
                    active
                      ? 'bg-primary-soft text-primary-soft-foreground before:absolute before:inset-y-2 before:left-0 before:w-1 before:rounded-r-full before:bg-primary'
                      : 'hover:bg-muted',
                  )}>
                  <span className="truncate font-mono text-xs font-medium">{item.slug}</span>
                  <span className={cn('truncate text-xs', active ? 'opacity-80' : 'text-muted-foreground')}>{item.name}</span>
                </button>
              </li>
            );
          })}
        </ul>
      </LoadState>
      <NamespaceModal vault={props.vault} open={adding} onOpenChange={setAdding} onCreated={props.onSelect} />
    </div>
  );
}

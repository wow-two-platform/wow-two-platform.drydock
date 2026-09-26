import { useState } from 'react';
import { KeyRound, Plus } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Badge, Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '@wow-two-beta/ui/presentation/display';
import { Alert, StatusIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { useSecretChanges, useVaultSecrets } from '@/application/secrets';
import { Measures } from '@/domain/common';
import { SecretState } from '@/domain/secrets';
import { LoadState, TableStyles } from '@/presentation/common/components';
import { SetSecretModal } from './SetSecretModal';

/** A namespace's secrets as metadata, with rotation and serving controls; `overdue` keys are due for rotation. */
export function SecretsTable(props: { vault: string; ns: string; overdue: ReadonlySet<string> }) {
  const secrets = useVaultSecrets(props.vault, props.ns);
  const changes = useSecretChanges(props.vault, props.ns);
  const [editing, setEditing] = useState<{ key?: string } | null>(null);
  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-end">
        <Button size="sm" leadingSlot={<Plus size={14} />} onClick={() => setEditing({})}>Add secret</Button>
      </div>
      {changes.error && <Alert severity="danger" description={changes.error.message} />}
      <LoadState loading={secrets.loading} error={secrets.error} empty={!secrets.data?.length}
        emptyTitle="No secrets yet" emptyDescription="Values are write-only; Wheelhouse shows only metadata."
        onRetry={() => void secrets.refetch()}>
        <Table density="compact" isHoverable containerClassName={TableStyles.scrollBox}>
          <TableHead className={TableStyles.stickyHead}>
            <TableRow>
              <TableHeaderCell>Key</TableHeaderCell><TableHeaderCell>State</TableHeaderCell>
              <TableHeaderCell>Version</TableHeaderCell><TableHeaderCell>Updated</TableHeaderCell>
              <TableHeaderCell><span className="sr-only">Actions</span></TableHeaderCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(secrets.data ?? []).map((secret) => {
              const disabled = secret.state === SecretState.Disabled;
              return (
                <TableRow key={secret.key}>
                  <TableCell>
                    <span className="block font-mono text-xs">{secret.key}</span>
                    {secret.description && <span className="block text-xs text-muted-foreground">{secret.description}</span>}
                  </TableCell>
                  <TableCell><StatusIndicator tone={disabled ? 'warning' : 'success'} label={secret.state} /></TableCell>
                  <TableCell>v{secret.version}</TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                    <span className="flex flex-col items-start gap-1">
                      <span title={new Date(secret.updatedAtUtc).toLocaleString()}>{Measures.age(secret.updatedAtUtc)}</span>
                      {props.overdue.has(secret.key) && <Badge variant="warning">rotation due</Badge>}
                    </span>
                  </TableCell>
                  <TableCell>
                    <div className="flex justify-end gap-1">
                      <Button variant="ghost" tone="neutral" size="sm" leadingSlot={<KeyRound size={14} />}
                        aria-label={`Rotate ${secret.key}`} title="Write a new version" onClick={() => setEditing({ key: secret.key })} />
                      <Button variant="ghost" tone={disabled ? 'neutral' : 'danger'} size="sm" isLoading={changes.loading}
                        onClick={() => void changes.setDisabled(secret.key, !disabled)}>
                        {disabled ? 'Enable' : 'Disable'}
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </LoadState>
      <SetSecretModal vault={props.vault} ns={props.ns} secretKey={editing?.key} open={editing !== null}
        onOpenChange={(open) => { if (!open) setEditing(null); }} />
    </div>
  );
}

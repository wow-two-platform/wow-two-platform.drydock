import { useState } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Badge, Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '@wow-two-beta/ui/presentation/display';
import { Alert, StatusIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { AlertModal } from '@wow-two-beta/ui/presentation/overlays';
import { useTokenChanges, useVaultTokens } from '@/application/secrets';
import { Measures } from '@/domain/common';
import type { OverdueToken, VaultToken } from '@/domain/secrets';
import { LoadState, TableStyles } from '@/presentation/common/components';
import { MintTokenModal } from './MintTokenModal';

/** A namespace's product tokens, with minting and revocation; `flags` marks tokens due for rotation. */
export function TokensTable(props: { vault: string; ns: string; flags: ReadonlyMap<string, OverdueToken['reason']> }) {
  const tokens = useVaultTokens(props.vault, props.ns);
  const changes = useTokenChanges(props.vault, props.ns);
  const [minting, setMinting] = useState(false);
  const [revoking, setRevoking] = useState<VaultToken | null>(null);

  async function revoke() {
    if (!revoking) return;
    if (await changes.revoke(revoking.id) !== null) setRevoking(null);
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-end">
        <Button size="sm" leadingSlot={<Plus size={14} />} onClick={() => setMinting(true)}>Mint token</Button>
      </div>
      <LoadState loading={tokens.loading} error={tokens.error} empty={!tokens.data?.length}
        emptyTitle="No product tokens" emptyDescription="Mint one per product that reads this namespace."
        onRetry={() => void tokens.refetch()}>
        <Table density="compact" isHoverable containerClassName={TableStyles.scrollBox}>
          <TableHead className={TableStyles.stickyHead}>
            <TableRow>
              <TableHeaderCell>Name</TableHeaderCell><TableHeaderCell>Created</TableHeaderCell>
              <TableHeaderCell>Expires</TableHeaderCell><TableHeaderCell>State</TableHeaderCell>
              <TableHeaderCell><span className="sr-only">Actions</span></TableHeaderCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(tokens.data ?? []).map((token) => (
              <TableRow key={token.id}>
                <TableCell>{token.name}</TableCell>
                <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                  <span title={new Date(token.createdAtUtc).toLocaleString()}>{Measures.age(token.createdAtUtc)}</span>
                </TableCell>
                <TableCell className="text-xs text-muted-foreground">
                  {token.expiresAtUtc ? new Date(token.expiresAtUtc).toLocaleString() : 'never'}
                </TableCell>
                <TableCell>
                  <span className="flex flex-wrap items-center gap-2">
                    <StatusIndicator tone={token.isRevoked ? 'neutral' : 'success'} label={token.isRevoked ? 'revoked' : 'active'} />
                    {props.flags.get(token.id) && (
                      <Badge variant={props.flags.get(token.id) === 'expired' ? 'danger' : 'warning'}>{props.flags.get(token.id)}</Badge>
                    )}
                  </span>
                </TableCell>
                <TableCell className="text-right">
                  {!token.isRevoked && (
                    <Button variant="ghost" tone="danger" size="sm" onClick={() => setRevoking(token)}>Revoke</Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </LoadState>
      <MintTokenModal vault={props.vault} ns={props.ns} open={minting} onOpenChange={setMinting} />
      <AlertModal open={revoking !== null} onOpenChange={(open) => { if (!open) { setRevoking(null); changes.reset(); } }}>
        <AlertModal.Content>
          <AlertModal.Header>
            <AlertModal.Title>Revoke {revoking?.name}?</AlertModal.Title>
            <AlertModal.Description>
              Later reads with this token fail. A running product keeps values it already loaded until it restarts.
            </AlertModal.Description>
          </AlertModal.Header>
          {changes.error && <AlertModal.Body><Alert severity="danger" description={changes.error.message} /></AlertModal.Body>}
          <AlertModal.Footer>
            <Button variant="outline" tone="neutral" onClick={() => setRevoking(null)}>Cancel</Button>
            <Button tone="danger" isLoading={changes.loading} onClick={() => void revoke()}>Revoke</Button>
          </AlertModal.Footer>
        </AlertModal.Content>
      </AlertModal>
    </div>
  );
}

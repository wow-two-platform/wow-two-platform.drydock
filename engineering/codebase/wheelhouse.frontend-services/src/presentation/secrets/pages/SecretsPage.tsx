import { useEffect, useState } from 'react';
import { KeyRound } from 'lucide-react';
import { Badge, EmptyState, Tabs, Text } from '@wow-two-beta/ui/presentation/display';
import { Select } from '@wow-two-beta/ui/presentation/forms';
import { useVaultHygiene, useVaults } from '@/application/secrets';
import { VaultStatus } from '@/domain/secrets';
import { LoadState, Panel } from '@/presentation/common/components';
import { NamespaceList } from '../components/NamespaceList';
import { SecretsTable } from '../components/SecretsTable';
import { TokensTable } from '../components/TokensTable';

const STATUS_VARIANT = { unsealed: 'success', sealed: 'warning', unreachable: 'danger' } as const;

const count = (amount: number, noun: string) => `${amount} ${noun}${amount === 1 ? '' : 's'}`;

/** Administers the code-owned vaults: namespaces, write-only secrets and product tokens. */
export function SecretsPage() {
  const vaults = useVaults();
  const [vault, setVault] = useState('');
  const [ns, setNs] = useState('');
  const selected = vaults.data?.find((item) => item.id === vault);
  const hygiene = useVaultHygiene(selected?.status === VaultStatus.Unsealed ? vault : '');
  const overdueSecrets = new Set(
    (hygiene.data?.overdueSecrets ?? []).filter((secret) => secret.namespace === ns).map((secret) => secret.key));
  const tokenFlags = new Map(
    (hygiene.data?.overdueTokens ?? []).filter((token) => token.namespace === ns).map((token) => [token.id, token.reason]));
  const dueCount = (hygiene.data?.overdueSecrets.length ?? 0) + (hygiene.data?.overdueTokens.length ?? 0);

  // ---- Default to the first vault once the catalog loads ----
  useEffect(() => {
    const first = vaults.data?.[0];
    if (!vault && first) setVault(first.id);
  }, [vaults.data, vault]);

  return (
    <LoadState loading={vaults.loading} error={vaults.error} empty={!vaults.data?.length}
      emptyTitle="No vaults configured" emptyDescription="Vaults are defined in reviewed code (fleet.py)."
      onRetry={() => void vaults.refetch()}>
      <Panel
        title={
          <span className="flex items-center gap-2">
            {selected?.name ?? 'Vault'}
            {selected && <Badge variant={STATUS_VARIANT[selected.status]}>{selected.status}</Badge>}
          </span>
        }
        description={
          dueCount > 0 && hygiene.data
            ? `Values are write-only. Due for rotation: ${count(hygiene.data.overdueSecrets.length, 'secret')} and ${count(hygiene.data.overdueTokens.length, 'token')}.`
            : 'Values are write-only: Wheelhouse sends them to the vault and shows only metadata.'
        }
        actions={(vaults.data?.length ?? 0) > 1 && (
          <Select<string> value={vault || null} onValueChange={(option) => { setVault(option?.value ?? ''); setNs(''); }}
            getOptionLabel={(key) => vaults.data?.find((item) => item.id === key)?.name ?? key}>
            <Select.Trigger size="sm" aria-label="Vault"><Select.Value placeholder="Select a vault" /></Select.Trigger>
            <Select.Content>
              {(vaults.data ?? []).map((item) => <Select.Item key={item.id} itemKey={item.id} label={item.name} />)}
            </Select.Content>
          </Select>
        )}
      >
        {selected && selected.status !== VaultStatus.Unsealed ? (
          <EmptyState size="sm" icon={<KeyRound size={24} />} title={`This vault is ${selected.status}`}
            description="Unseal it or restore connectivity from Wheelhouse before administering it." />
        ) : vault && (
          <div className="grid gap-6 xl:grid-cols-[14rem_1fr]">
            <NamespaceList vault={vault} selected={ns} onSelect={setNs} />
            {ns ? (
              <Tabs defaultValue="secrets" className="min-w-0">
                <Tabs.List>
                  <Tabs.Tab value="secrets">Secrets</Tabs.Tab>
                  <Tabs.Tab value="tokens">Tokens</Tabs.Tab>
                </Tabs.List>
                <Tabs.Panel value="secrets" className="pt-4"><SecretsTable vault={vault} ns={ns} overdue={overdueSecrets} /></Tabs.Panel>
                <Tabs.Panel value="tokens" className="pt-4"><TokensTable vault={vault} ns={ns} flags={tokenFlags} /></Tabs.Panel>
              </Tabs>
            ) : (
              <Text size="sm" color="muted" className="self-center">Select a namespace to manage its secrets and tokens.</Text>
            )}
          </div>
        )}
      </Panel>
    </LoadState>
  );
}

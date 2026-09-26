import { useState } from 'react';
import { Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '@wow-two-beta/ui/presentation/display';
import { Select } from '@wow-two-beta/ui/presentation/forms';
import { useDeploymentTargets } from '@/application/deployments';
import { useServers } from '@/application/fleet';
import { VpsProvider } from '@/domain/fleet';
import { LoadState, Panel } from '@/presentation/common/components';
import { VitalsPanel } from '../components/VitalsPanel';

/** The code-owned hosts and the product environments bound to them. */
export function FleetPage() {
  const servers = useServers();
  const targets = useDeploymentTargets();
  const [provider, setProvider] = useState<VpsProvider | null>(null);
  const visible = (servers.data ?? []).filter((server) => !provider || server.provider === provider);
  return (
    <div className="flex flex-col gap-6">
      <VitalsPanel />
      <Panel title="Hosts" description="Defined in reviewed code; adding one needs a code change and a rebuild."
        actions={
          <Select<VpsProvider> value={provider} isClearable clearLabel="All providers"
            onValueChange={(option) => setProvider(option?.value ?? null)}>
            <Select.Trigger size="sm" aria-label="Provider"><Select.Value placeholder="All providers" /></Select.Trigger>
            <Select.Content>
              {Object.values(VpsProvider).map((value) => <Select.Item key={value} itemKey={value} label={value} />)}
            </Select.Content>
          </Select>
        }>
        <LoadState loading={servers.loading} error={servers.error} empty={!visible.length}
          emptyTitle="No configured hosts" emptyDescription="No host matches this provider." onRetry={() => void servers.refetch()}>
          <Table density="compact" isHoverable>
            <TableHead>
              <TableRow>
                <TableHeaderCell>Name</TableHeaderCell><TableHeaderCell>Provider</TableHeaderCell>
                <TableHeaderCell>Host</TableHeaderCell><TableHeaderCell>Region</TableHeaderCell>
                <TableHeaderCell>SSH user</TableHeaderCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {visible.map((server) => (
                <TableRow key={server.id}>
                  <TableCell>{server.name}</TableCell><TableCell>{server.provider}</TableCell>
                  <TableCell className="font-mono text-xs">{server.host}</TableCell><TableCell>{server.region}</TableCell>
                  <TableCell className="font-mono text-xs">{server.sshUser}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </LoadState>
      </Panel>
      <Panel title="Environments" description="Each binds one product environment to one host.">
        <LoadState loading={targets.loading} error={targets.error} empty={!targets.data?.length}
          emptyTitle="No deployment targets" onRetry={() => void targets.refetch()}>
          <Table density="compact" isHoverable>
            <TableHead>
              <TableRow>
                <TableHeaderCell>Target</TableHeaderCell><TableHeaderCell>Product</TableHeaderCell>
                <TableHeaderCell>Environment</TableHeaderCell><TableHeaderCell>Host</TableHeaderCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(targets.data ?? []).map((target) => (
                <TableRow key={target.id}>
                  <TableCell className="font-mono text-xs">{target.id}</TableCell><TableCell>{target.product}</TableCell>
                  <TableCell>{target.environment}</TableCell><TableCell className="font-mono text-xs">{target.host}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </LoadState>
      </Panel>
    </div>
  );
}

import { useState } from 'react';
import { Server as ServerIcon } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Card, EmptyState, Heading, Text } from '@wow-two-beta/ui/presentation/display';
import { Alert, Spinner } from '@wow-two-beta/ui/presentation/feedback';
import { useServers } from '../hooks/useServers';
import { VpsProvider } from '../api/types';

/** Read-only hosts supplied by the reviewed fleet catalog. */
export function ServersPanel() {
  const { servers, loading, error, reload } = useServers();
  const [provider, setProvider] = useState<VpsProvider | ''>('');
  const visible = servers.filter(server => !provider || server.provider === provider);
  return (
    <Card className="border border-border">
      <div className="flex items-center justify-between border-b border-border p-5">
        <div>
          <Heading level={2} size="md">Servers</Heading>
          <Text color="muted" size="sm">Configured deployment hosts</Text>
        </div>
        <label className="flex items-center gap-2 text-sm">
          Provider
          <select className="rounded border border-border bg-background p-2" value={provider}
            onChange={event => setProvider(event.target.value as VpsProvider | '')}>
            <option value="">All providers</option>
            {Object.values(VpsProvider).map(value => <option key={value} value={value}>{value}</option>)}
          </select>
        </label>
      </div>
      <div className="p-5">
        {loading ? <Spinner label="Loading servers" /> : error ? (
          <Alert severity="danger" title="Couldn't load servers" description={error}
            actions={<Button variant="soft" tone="danger" onClick={() => void reload()}>Retry</Button>} />
        ) : !visible.length ? (
          <EmptyState icon={<ServerIcon size={28} />} title="No configured servers"
            description="No deployment hosts are available for this provider." />
        ) : (
          <table className="w-full text-left text-sm">
            <thead><tr><th>Name</th><th>Provider</th><th>Host</th><th>Region</th></tr></thead>
            <tbody>{visible.map(server => (
              <tr key={server.id} className="border-t border-border">
                <td className="py-3">{server.name}</td><td>{server.provider}</td>
                <td className="font-mono text-xs">{server.host}</td><td>{server.region}</td>
              </tr>
            ))}</tbody>
          </table>
        )}
      </div>
    </Card>
  );
}

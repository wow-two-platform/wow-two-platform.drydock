import { useState } from 'react';
import { Plus, Server as ServerIcon } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/actions';
import { Badge, Card, EmptyState, Heading, Text } from '@wow-two-beta/ui/display';
import { Alert, Spinner } from '@wow-two-beta/ui/feedback';
import { useServers } from '../hooks/useServers';
import { RegisterServerForm } from './RegisterServerForm';
import type { ServerStatus } from '../api/types';

type BadgeTone = 'neutral' | 'success' | 'danger' | 'warning';

const STATUS_TONE: Record<ServerStatus, BadgeTone> = {
  Reachable: 'success',
  Unreachable: 'danger',
  Provisioning: 'warning',
  Unknown: 'neutral',
};

/** The Servers registry — list deploy targets and register new ones. */
export function ServersPanel() {
  const { servers, loading, error, reload } = useServers();
  const [showForm, setShowForm] = useState(false);

  return (
    <Card className="border border-border">
      <div className="flex items-center justify-between border-b border-border p-5">
        <div>
          <Heading level={2} size="md">
            Servers
          </Heading>
          <Text color="muted" size="sm">
            Hetzner VPS deploy targets
          </Text>
        </div>
        <Button
          variant="solid"
          tone="primary"
          size="sm"
          leadingSlot={<Plus size={16} />}
          onClick={() => setShowForm((v) => !v)}
        >
          {showForm ? 'Close' : 'Register server'}
        </Button>
      </div>

      {showForm && (
        <div className="border-b border-border p-5">
          <RegisterServerForm
            onRegistered={() => {
              setShowForm(false);
              void reload();
            }}
            onCancel={() => setShowForm(false)}
          />
        </div>
      )}

      <div className="p-5">
        {loading ? (
          <div className="flex justify-center py-10">
            <Spinner label="Loading servers" />
          </div>
        ) : error ? (
          <Alert
            severity="danger"
            title="Couldn't load servers"
            description={error}
            actions={
              <Button variant="soft" tone="danger" size="sm" onClick={() => void reload()}>
                Retry
              </Button>
            }
          />
        ) : servers.length === 0 ? (
          <EmptyState
            icon={<ServerIcon size={28} />}
            title="No servers yet"
            description="Register your first Hetzner box to start deploying."
            actions={
              <Button variant="solid" tone="primary" size="sm" onClick={() => setShowForm(true)}>
                Register server
              </Button>
            }
          />
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border text-left text-xs uppercase tracking-wide text-muted-foreground">
                <th className="pb-2 font-medium">Name</th>
                <th className="pb-2 font-medium">Host</th>
                <th className="pb-2 font-medium">User</th>
                <th className="pb-2 font-medium">Region</th>
                <th className="pb-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {servers.map((s) => (
                <tr key={s.id} className="border-b border-border/60 last:border-0">
                  <td className="py-3 font-medium">{s.name}</td>
                  <td className="py-3 font-mono text-xs text-muted-foreground">{s.host}</td>
                  <td className="py-3 text-muted-foreground">{s.sshUser}</td>
                  <td className="py-3 text-muted-foreground">{s.region ?? '—'}</td>
                  <td className="py-3">
                    <Badge variant={STATUS_TONE[s.status]}>{s.status}</Badge>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </Card>
  );
}

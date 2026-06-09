import { useEffect, useState } from 'react';
import { Badge, Heading, Text } from '@wow-two-beta/ui/display';
import { ServersPanel } from './components/ServersPanel';
import { api } from './api/client';

type Connection = 'checking' | 'online' | 'offline';

/** Root of the Drydock control-plane dashboard. */
export default function App() {
  const [connection, setConnection] = useState<Connection>('checking');

  useEffect(() => {
    const controller = new AbortController();
    api
      .getStatus(controller.signal)
      .then(() => setConnection('online'))
      .catch(() => setConnection('offline'));
    return () => controller.abort();
  }, []);

  return (
    <div className="min-h-full bg-background text-foreground">
      <header className="border-b border-border bg-card/40">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <div>
            <Heading level={1} size="lg">
              Drydock
            </Heading>
            <Text color="muted" size="sm">
              Product ops &amp; deploy control plane
            </Text>
          </div>
          <Badge
            variant={connection === 'online' ? 'success' : connection === 'offline' ? 'danger' : 'neutral'}
          >
            {connection === 'online' ? 'API connected' : connection === 'offline' ? 'API offline' : 'Connecting…'}
          </Badge>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-6 py-8">
        <ServersPanel />
      </main>
    </div>
  );
}

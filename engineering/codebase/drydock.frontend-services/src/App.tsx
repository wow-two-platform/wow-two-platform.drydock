import { useEffect, useState } from 'react';
import { LogOut } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/actions';
import { Avatar, Badge, Heading, Text } from '@wow-two-beta/ui/display';
import { Spinner } from '@wow-two-beta/ui/feedback';
import { ProductsPanel } from './components/ProductsPanel';
import { ServersPanel } from './components/ServersPanel';
import { SignInScreen } from './components/SignInScreen';
import { useAuth } from './hooks/useAuth';
import { api } from './api/client';
import type { CurrentUser } from './api/types';

type Connection = 'checking' | 'online' | 'offline';

/** Root of the Drydock control-plane dashboard — gated behind single-admin GitHub sign-in. */
export default function App() {
  const { user, loading, signIn, signOut } = useAuth();

  if (loading) {
    return (
      <div className="flex min-h-full items-center justify-center bg-background text-foreground">
        <Spinner label="Loading Drydock" />
      </div>
    );
  }

  if (!user) {
    return <SignInScreen onSignIn={signIn} />;
  }

  return <Dashboard user={user} onSignOut={() => void signOut()} />;
}

/** The authenticated dashboard — header (user + connection + logout) and the control-plane panels. */
function Dashboard({ user, onSignOut }: { user: CurrentUser; onSignOut: () => void }) {
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

          <div className="flex items-center gap-4">
            <Badge
              variant={connection === 'online' ? 'success' : connection === 'offline' ? 'danger' : 'neutral'}
            >
              {connection === 'online' ? 'API connected' : connection === 'offline' ? 'API offline' : 'Connecting…'}
            </Badge>

            <div className="flex items-center gap-2">
              <Avatar src={user.avatar ?? undefined} name={user.name || user.login} size="sm" />
              <Text size="sm" className="hidden sm:inline">
                {user.name || user.login}
              </Text>
            </div>

            <Button
              variant="ghost"
              tone="neutral"
              size="sm"
              leadingSlot={<LogOut size={16} />}
              onClick={onSignOut}
            >
              Sign out
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto flex max-w-5xl flex-col gap-8 px-6 py-8">
        <ProductsPanel />
        <ServersPanel />
      </main>
    </div>
  );
}

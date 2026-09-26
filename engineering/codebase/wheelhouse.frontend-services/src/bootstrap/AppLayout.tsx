import { useState, type ReactNode } from 'react';
import { useLocation } from 'react-router-dom';
import { Menu, Ship } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Badge, Heading, Text } from '@wow-two-beta/ui/presentation/display';
import { AppShell } from '@wow-two-beta/ui/presentation/layout';
import { useApiConnection } from '@/application/system';
import type { CurrentUser } from '@/domain/auth';
import { PageActionsOutlet, PageActionsProvider } from '@/presentation/common/components';
import { Sidebar, SidebarToggle, useColorScheme, useSidebarCollapsed } from '@/presentation/shell';
import { APP_ROUTES } from './routes';

/** Props for {@link AppLayout}. */
export interface AppLayoutProps {
  user: CurrentUser;
  onSignOut: () => void;
  children: ReactNode;
}

/** The signed-in frame: a full-height sidebar, and the routed page under its own header. */
export function AppLayout(props: AppLayoutProps) {
  const { connection, localRig } = useApiConnection();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);
  const { collapsed, toggle } = useSidebarCollapsed();
  const { scheme, setScheme } = useColorScheme();
  const current = APP_ROUTES.find((route) => route.path === location.pathname);
  const sidebarWidth = collapsed ? '68px' : '256px';
  return (
    <PageActionsProvider>
    <AppShell sidebarWidth={sidebarWidth} isSidebarOpen={menuOpen} onSidebarOpenChange={setMenuOpen}
      className="transition-[grid-template-columns] duration-150 ease-[cubic-bezier(0.2,0,0,1)] motion-reduce:transition-none">
      {/* Small screens only: the sidebar becomes a drawer behind this bar. */}
      <AppShell.Header className="lg:hidden">
        <Button variant="ghost" tone="neutral" size="sm" aria-label="Open navigation"
          leadingSlot={<Menu size={18} />} onClick={() => setMenuOpen(true)} />
        <span className="flex size-7 items-center justify-center rounded-md bg-sidebar-accent text-sidebar-deep">
          <Ship size={16} />
        </span>
        <span className="font-semibold">Wheelhouse</span>
        {localRig && <Badge className="ml-auto whitespace-nowrap" variant="warning">Local rig</Badge>}
      </AppShell.Header>
      {/* Below lg the shell renders a padded drawer; the navy panel covers that padding edge to edge. */}
      <AppShell.Sidebar className="top-0 h-svh overflow-x-hidden border-r-0 bg-sidebar text-sidebar-foreground max-lg:-m-6 max-lg:h-[calc(100%+3rem)] max-lg:p-4">
        <Sidebar links={APP_ROUTES} activePath={current?.path} collapsed={collapsed}
          onNavigate={() => setMenuOpen(false)} localRig={localRig} user={props.user} connection={connection}
          scheme={scheme} onSchemeChange={setScheme} onSignOut={props.onSignOut} />
      </AppShell.Sidebar>
      <SidebarToggle collapsed={collapsed} width={sidebarWidth} onToggle={toggle} />
      <AppShell.Main className="min-w-0">
        <AppShell.Content className="p-4 sm:p-6 lg:px-10 lg:py-8">
          <div className="mx-auto flex max-w-6xl flex-col gap-6">
            {current && (
              <header className="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
                <div className="flex min-w-0 flex-col gap-1">
                  <Heading level={1} size="xl">{current.label}</Heading>
                  <Text color="muted">{current.description}</Text>
                </div>
                <PageActionsOutlet className="flex flex-wrap items-center gap-2 empty:hidden" />
              </header>
            )}
            {/* Each page fades in as it opens. */}
            <div key={location.pathname} className="flex flex-col gap-6 motion-safe:animate-(--animate-fade-in)">
              {props.children}
            </div>
          </div>
        </AppShell.Content>
      </AppShell.Main>
    </AppShell>
    </PageActionsProvider>
  );
}

import type { ComponentType } from 'react';
import { NavLink } from 'react-router-dom';
import { Ship } from 'lucide-react';
import { cn } from '@wow-two-beta/ui/foundation/utils';
import { Tooltip } from '@wow-two-beta/ui/presentation/display';
import { useAppShell } from '@wow-two-beta/ui/presentation/layout';
import type { CurrentUser } from '@/domain/auth';
import type { ColorScheme } from '../hooks/useColorScheme';
import { ProfileMenu, type ApiConnection } from './ProfileMenu';

/** One place in the sidebar. */
export interface SidebarLink {
  path: string;
  label: string;
  icon: ComponentType<{ size?: number; className?: string }>;
}

/** Props for {@link Sidebar}. */
export interface SidebarProps {
  links: readonly SidebarLink[];
  activePath: string | undefined;
  /** The operator's choice of a narrow icon rail; ignored in the mobile drawer. */
  collapsed: boolean;
  onNavigate: () => void;
  localRig: boolean;
  user: CurrentUser;
  connection: ApiConnection;
  scheme: ColorScheme;
  onSchemeChange: (scheme: ColorScheme) => void;
  onSignOut: () => void;
}

// Labels fade instead of unmounting, and every row keeps its height, so nothing moves while the width animates.
const LABEL = 'whitespace-nowrap transition-opacity duration-100';
const labelVisibility = (isRail: boolean) => (isRail ? 'pointer-events-none opacity-0' : 'opacity-100');

/** Brand, environment, places and the operator — the wheelhouse beside every page. */
export function Sidebar(props: SidebarProps) {
  // Below the shell's breakpoint the sidebar is a drawer, which always shows labels.
  const inDrawer = useAppShell().isSidebarCollapsed;
  const isRail = props.collapsed && !inDrawer;
  return (
    <div className="flex h-full flex-col gap-4">
      <div className="flex h-10 items-center gap-3 px-0.5">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-deep shadow-sm">
          <Ship size={20} />
        </span>
        <span className={cn(LABEL, labelVisibility(isRail), 'text-base font-semibold tracking-tight text-white')}>Wheelhouse</span>
      </div>

      {props.localRig && <RigRow isRail={isRail} />}

      <ul className="flex flex-col gap-1">
        {props.links.map((link) => (
          <li key={link.path}>
            <SidebarItem link={link} active={link.path === props.activePath} isRail={isRail} onNavigate={props.onNavigate} />
          </li>
        ))}
      </ul>

      <div className="mt-auto border-t border-sidebar-border pt-3">
        <ProfileMenu user={props.user} connection={props.connection} isRail={isRail} scheme={props.scheme}
          onSchemeChange={props.onSchemeChange} onSignOut={props.onSignOut} />
      </div>
    </div>
  );
}

// ---- Environment ----

function RigRow(props: { isRail: boolean }) {
  const row = (
    <div className="flex h-8 items-center gap-3 rounded-lg border border-sidebar-accent/35 bg-sidebar-accent/10 px-3 text-xs"
      title="Deployments go to the local rehearsal target, not real hosts">
      <span aria-hidden className="size-2 shrink-0 rounded-full bg-sidebar-accent" />
      <span className={cn(LABEL, labelVisibility(props.isRail), 'font-semibold text-sidebar-accent')}>Local rig</span>
    </div>
  );
  return props.isRail
    ? <Tooltip content="Local rig — deployments go to the rehearsal target" placement="right">{row}</Tooltip>
    : row;
}

// ---- Item ----

function SidebarItem(props: { link: SidebarLink; active: boolean; isRail: boolean; onNavigate: () => void }) {
  const Icon = props.link.icon;
  const item = (
    <NavLink to={props.link.path} end onClick={props.onNavigate} aria-label={props.link.label}
      className={cn(
        'relative flex h-10 items-center gap-3 rounded-lg px-3 text-sm font-medium transition-colors',
        props.active
          ? 'bg-sidebar-active text-white before:absolute before:inset-y-2 before:left-0 before:w-1 before:rounded-r-full before:bg-sidebar-accent'
          : 'text-sidebar-muted hover:bg-sidebar-hover hover:text-sidebar-foreground',
      )}>
      <Icon size={18} className={cn('shrink-0', props.active ? 'text-sidebar-accent' : '')} />
      <span className={cn(LABEL, labelVisibility(props.isRail))}>{props.link.label}</span>
    </NavLink>
  );
  return props.isRail ? <Tooltip content={props.link.label} placement="right">{item}</Tooltip> : item;
}

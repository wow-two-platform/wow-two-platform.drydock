import { Check, ChevronsUpDown, LogOut, Monitor, Moon, Sun, type LucideIcon } from 'lucide-react';
import { cn } from '@wow-two-beta/ui/foundation/utils';
import { Avatar } from '@wow-two-beta/ui/presentation/display';
import { DropdownMenu } from '@wow-two-beta/ui/presentation/nav';
import type { CurrentUser } from '@/domain/auth';
import { ColorScheme } from '../hooks/useColorScheme';

/** Whether the management API answers. */
export type ApiConnection = 'online' | 'offline' | 'checking';

/** Props for {@link ProfileMenu}. */
export interface ProfileMenuProps {
  user: CurrentUser;
  connection: ApiConnection;
  /** The sidebar is a narrow icon rail. */
  isRail: boolean;
  scheme: ColorScheme;
  onSchemeChange: (scheme: ColorScheme) => void;
  onSignOut: () => void;
}

const CONNECTION: Record<ApiConnection, { dot: string; label: string }> = {
  online: { dot: 'bg-success', label: 'API connected' },
  offline: { dot: 'bg-destructive', label: 'API offline' },
  checking: { dot: 'bg-sidebar-muted', label: 'Connecting…' },
};

const SCHEMES: readonly { value: ColorScheme; label: string; icon: LucideIcon }[] = [
  { value: ColorScheme.System, label: 'System', icon: Monitor },
  { value: ColorScheme.Light, label: 'Light', icon: Sun },
  { value: ColorScheme.Dark, label: 'Dark', icon: Moon },
];

/** The signed-in operator at the foot of the sidebar; the theme and sign-out live in its menu. */
export function ProfileMenu(props: ProfileMenuProps) {
  const name = props.user.name || props.user.login;
  const status = CONNECTION[props.connection];
  return (
    <DropdownMenu placement={props.isRail ? 'right-end' : 'top-start'}>
      <DropdownMenu.Trigger asChild>
        <button type="button" aria-label={`Account menu for ${name}`}
          className={cn(
            'flex w-full items-center gap-3 overflow-hidden rounded-lg px-1.5 py-2 text-left text-sidebar-foreground transition-colors',
            'hover:bg-sidebar-hover focus-visible:outline-2 focus-visible:outline-sidebar-accent',
          )}>
          <span className="relative shrink-0">
            <Avatar {...(props.user.avatar ? { src: props.user.avatar } : {})} name={name} size="sm" />
            <span aria-hidden className={cn('absolute -bottom-0.5 -right-0.5 size-2.5 rounded-full ring-2 ring-sidebar', status.dot)} />
          </span>
          {/* Text fades rather than unmounting, so the avatar holds its place while the rail animates. */}
          <span className={cn('flex min-w-0 flex-1 items-center gap-2 whitespace-nowrap transition-opacity duration-150',
            props.isRail ? 'pointer-events-none opacity-0' : 'opacity-100')}>
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-medium">{name}</span>
              <span className="block truncate text-xs text-sidebar-muted">@{props.user.login}</span>
            </span>
            <ChevronsUpDown size={16} className="shrink-0 text-sidebar-muted" />
          </span>
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Content aria-label="Account" className="w-64">
        <DropdownMenu.Label>
          <span className="block truncate text-sm font-medium text-foreground">{name}</span>
          <span className="block truncate text-xs text-muted-foreground">Signed in with GitHub as @{props.user.login}</span>
        </DropdownMenu.Label>
        <DropdownMenu.Separator />
        <div className="flex items-center gap-2 px-2 py-1.5 text-xs text-muted-foreground">
          <span aria-hidden className={cn('size-2 rounded-full', status.dot)} />
          {status.label}
        </div>
        <DropdownMenu.Separator />
        <DropdownMenu.Group label="Theme">
          {SCHEMES.map((option) => (
            <DropdownMenu.Item key={option.value} onSelect={() => props.onSchemeChange(option.value)}>
              <option.icon size={14} />
              {option.label}
              {props.scheme === option.value && (
                <>
                  <Check size={14} aria-hidden className="ml-auto text-primary" />
                  <span className="sr-only">, current</span>
                </>
              )}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Group>
        <DropdownMenu.Separator />
        <DropdownMenu.Item state="destructive" onSelect={props.onSignOut}>
          <LogOut size={14} />
          Sign out
        </DropdownMenu.Item>
      </DropdownMenu.Content>
    </DropdownMenu>
  );
}

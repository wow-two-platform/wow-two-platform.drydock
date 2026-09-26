import type { ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';
import { cn } from '@wow-two-beta/ui/foundation/utils';
import { TrendIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { Skeleton } from '@/presentation/common/skeleton';

/** The tint behind a tile's icon, one per kind of figure. */
export type KpiTint = 'primary' | 'success' | 'info' | 'warning' | 'accent';

const TINT: Record<KpiTint, string> = {
  primary: 'bg-primary-soft text-primary-soft-foreground',
  success: 'bg-success-soft text-success-soft-foreground',
  info: 'bg-info-soft text-info-soft-foreground',
  warning: 'bg-warning-soft text-warning-soft-foreground',
  accent: 'bg-accent-soft text-accent-soft-foreground',
};

/** A figure's change against the window before it. */
export interface KpiTrend {
  value: number;
  format: (value: number) => ReactNode;
  /** Higher is worse, as for a rollout time. */
  isInverse?: boolean;
  label: ReactNode;
}

/** Props for {@link KpiTile}. */
export interface KpiTileProps {
  icon: LucideIcon;
  tint: KpiTint;
  label: string;
  value: ReactNode;
  helper?: ReactNode;
  /** Omitted when either window has no value to compare. */
  trend?: KpiTrend | null;
}

/** One headline figure: a tinted icon and label, the value with its trend, and a helper line; values swap inside a `Skeleton.Group`. */
export function KpiTile(props: KpiTileProps) {
  return (
    <div className="flex flex-col gap-3 rounded-xl border border-border bg-card p-4">
      <div className="flex items-center gap-3">
        <span aria-hidden className={cn('flex size-9 shrink-0 items-center justify-center rounded-lg', TINT[props.tint])}>
          <props.icon size={18} />
        </span>
        <span className="text-sm font-medium text-muted-foreground">{props.label}</span>
      </div>
      <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
        <Skeleton.Slot className="text-3xl font-semibold tracking-tight tabular-nums">{props.value}</Skeleton.Slot>
        {props.trend && (
          <Skeleton.Slot>
            <TrendIndicator value={props.trend.value} format={props.trend.format} isInverse={props.trend.isInverse ?? false}
              label={props.trend.label} />
          </Skeleton.Slot>
        )}
      </div>
      {props.helper && <Skeleton.Slot className="text-xs text-muted-foreground">{props.helper}</Skeleton.Slot>}
    </div>
  );
}

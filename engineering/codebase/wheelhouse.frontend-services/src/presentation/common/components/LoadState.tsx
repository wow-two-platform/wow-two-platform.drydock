import type { ReactNode } from 'react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { EmptyState } from '@wow-two-beta/ui/presentation/display';
import { Alert, Spinner } from '@wow-two-beta/ui/presentation/feedback';

/** Props for {@link LoadState}. */
export interface LoadStateProps {
  loading: boolean;
  error: { message: string } | null;
  empty: boolean;
  emptyTitle: string;
  emptyDescription?: string;
  emptyIcon?: ReactNode;
  /** The empty state's next step, such as a create button. */
  emptyActions?: ReactNode;
  onRetry?: () => void;
  /** The first load's placeholder, shaped like the content; a spinner when absent. */
  skeleton?: ReactNode;
  children: ReactNode;
}

/** Renders loading, failure and empty states before the loaded content. */
export function LoadState(props: LoadStateProps) {
  if (props.loading)
    return props.skeleton ?? <div className="flex justify-center py-8"><Spinner label="Loading" /></div>;
  if (props.error)
    return (
      <Alert severity="danger" title="Couldn't load" description={props.error.message}
        actions={props.onRetry && <Button variant="soft" tone="danger" size="sm" onClick={props.onRetry}>Retry</Button>} />
    );
  if (props.empty)
    return (
      <EmptyState size="sm" icon={props.emptyIcon} title={props.emptyTitle} description={props.emptyDescription}
        actions={props.emptyActions} />
    );
  return <>{props.children}</>;
}

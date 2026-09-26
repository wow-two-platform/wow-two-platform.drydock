import { createContext, forwardRef, useContext, useMemo, type ComponentPropsWithoutRef } from 'react';
import { cn } from '@wow-two-beta/ui/foundation/utils';
import { Skeleton as SdkSkeleton } from '@wow-two-beta/ui/presentation/feedback';

// Shim of Skeleton.Group / Skeleton.Slot from @wow-two-beta/ui (added in the SDK, unpublished at 0.0.108).
// Same API; swap the import to the SDK on the next re-pin and delete this folder.

const GroupContext = createContext<{ loading: boolean } | null>(null);

/** Props for {@link SkeletonGroup}. */
export interface SkeletonGroupProps extends ComponentPropsWithoutRef<'div'> {
  loading: boolean;
  label?: string;
}

/** A loading region: `aria-busy`, one announcement, and every slot inside switching together. */
export const SkeletonGroup = forwardRef<HTMLDivElement, SkeletonGroupProps>(
  ({ loading, label = 'Loading…', children, ...props }, ref) => {
    const value = useMemo(() => ({ loading }), [loading]);
    return (
      <GroupContext.Provider value={value}>
        <div ref={ref} aria-busy={loading || undefined} {...props}>
          {loading && <span role="status" className="sr-only">{label}</span>}
          {children}
        </div>
      </GroupContext.Provider>
    );
  },
);
SkeletonGroup.displayName = 'Skeleton.Group';

/** Props for {@link SkeletonSlot}. */
export interface SkeletonSlotProps extends ComponentPropsWithoutRef<'span'> {
  loading?: boolean;
  block?: boolean;
}

/** Real content, invisible while loading, on a placeholder exactly its size. */
export const SkeletonSlot = forwardRef<HTMLElement, SkeletonSlotProps>(
  ({ loading, block = false, className, children, ...props }, ref) => {
    const group = useContext(GroupContext);
    const isLoading = loading ?? group?.loading ?? false;
    const Tag = block ? 'div' : 'span';
    return (
      <Tag ref={ref as never} aria-hidden={isLoading || undefined} data-loading={isLoading || undefined}
        // The placeholder merges last, so a caller's text colour cannot show through it.
        className={cn(block ? 'block' : 'inline-block', className,
          isLoading && 'pointer-events-none animate-pulse select-none rounded-sm bg-muted text-transparent motion-reduce:animate-none [&_*]:invisible')}
        {...props}>
        {children}
      </Tag>
    );
  },
);
SkeletonSlot.displayName = 'Skeleton.Slot';

// A local block, so the parts attach here rather than onto the SDK's own export.
const SkeletonBlock = forwardRef<HTMLDivElement, ComponentPropsWithoutRef<typeof SdkSkeleton>>((props, ref) => (
  <SdkSkeleton ref={ref} {...props} />
));
SkeletonBlock.displayName = 'Skeleton';

/** The SDK block plus the Group and Slot parts, as the SDK exposes them. */
export const Skeleton = Object.assign(SkeletonBlock, { Group: SkeletonGroup, Slot: SkeletonSlot });

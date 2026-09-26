import { useEffect, useState, type ReactNode, type TransitionEvent } from 'react';
import { cn } from '@wow-two-beta/ui/foundation/utils';

/**
 * The open/close lifecycle of an expanding region: content mounts collapsed and expands on the next frame, and
 * stays mounted until the collapse ends, so both directions animate.
 */
export function useExpand(open: boolean) {
  const [mounted, setMounted] = useState(open);
  const [expanded, setExpanded] = useState(open);

  useEffect(() => {
    if (!open) {
      setExpanded(false);
      return;
    }
    setMounted(true);
    const frame = requestAnimationFrame(() => setExpanded(true));
    return () => cancelAnimationFrame(frame);
  }, [open]);

  return {
    mounted,
    /** Props for the element whose rows animate; its only child wraps the content. */
    region: {
      className: cn(
        'grid transition-[grid-template-rows,opacity] duration-200 ease-[cubic-bezier(0.2,0,0,1)] motion-reduce:transition-none',
        expanded ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0',
      ),
      inert: !open || undefined,
      onTransitionEnd: (event: TransitionEvent<HTMLElement>) => {
        if (event.target === event.currentTarget && !open) setMounted(false);
      },
    },
  } as const;
}

/** Props for {@link Expand}. */
export interface ExpandProps {
  open: boolean;
  children: ReactNode;
  className?: string;
}

/** Opens and closes its content by height and opacity; reduced motion switches instantly. */
export function Expand(props: ExpandProps) {
  const expand = useExpand(props.open);
  if (!expand.mounted) return null;
  return (
    <div {...expand.region} className={cn(expand.region.className, props.className)}>
      <div className="min-h-0 overflow-hidden">{props.children}</div>
    </div>
  );
}

import { ChevronLeft, ChevronRight } from 'lucide-react';

/** Props for {@link SidebarToggle}. */
export interface SidebarToggleProps {
  collapsed: boolean;
  /** The sidebar's current width, e.g. `256px`; the handle straddles its edge. */
  width: string;
  onToggle: () => void;
}

/** A slim handle on the sidebar's edge, beside the logo; it glides with the width, so no row moves. */
export function SidebarToggle(props: SidebarToggleProps) {
  const label = props.collapsed ? 'Expand the sidebar' : 'Collapse the sidebar';
  return (
    <button type="button" onClick={props.onToggle} aria-label={label} title={label + ' (Ctrl/⌘ B)'}
      style={{ left: `calc(${props.width} - 12px)` }}
      className="fixed top-4 z-30 hidden h-9 w-6 items-center justify-center rounded-full border border-border bg-card text-muted-foreground shadow-sm transition-[left,color,border-color] duration-200 ease-out hover:border-primary/40 hover:text-primary focus-visible:outline-2 focus-visible:outline-ring motion-reduce:transition-none lg:flex">
      {props.collapsed ? <ChevronRight size={14} /> : <ChevronLeft size={14} />}
    </button>
  );
}

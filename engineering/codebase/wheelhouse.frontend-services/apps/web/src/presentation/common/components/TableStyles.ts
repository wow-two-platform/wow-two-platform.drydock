/**
 * A list that can grow: it scrolls inside a bounded box and keeps its header in view. The SDK head is
 * translucent, so the sticky one takes the opaque muted surface.
 */
export const TableStyles = {
  /** For `Table`'s `containerClassName`. */
  scrollBox: 'max-h-[28rem] overflow-y-auto',
  /** For `TableHead`'s `className`. */
  stickyHead: 'sticky top-0 z-10 bg-muted',
} as const;

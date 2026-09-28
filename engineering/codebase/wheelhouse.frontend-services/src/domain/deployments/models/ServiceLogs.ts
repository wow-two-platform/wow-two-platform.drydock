/** The last lines one service's container wrote, read on request; Wheelhouse never stores them. */
export interface ServiceLogs {
  targetId: string;
  service: string;
  tail: number;
  collectedAt: string;
  /** Oldest first, each prefixed with its container timestamp. */
  lines: string[];
  /** Whether any line was cut at the runner's line-length limit. */
  truncated: boolean;
}

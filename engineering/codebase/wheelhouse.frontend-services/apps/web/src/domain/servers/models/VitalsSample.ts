/** One target's stored reading; a figure is null where the host hid it or the target could not be read. */
export interface VitalsSample {
  targetId: string;
  serverId: string;
  sampledAt: string;
  readable: boolean;
  loadPercent: number | null;
  memoryPercent: number | null;
  diskPercent: number | null;
  containers: number;
  healthyContainers: number;
  restarts: number;
}

/** The trend ranges the servers page offers, in hours. */
export const TrendRanges = { day: 24, week: 168, month: 720 } as const;
export type TrendRange = keyof typeof TrendRanges;

/** One series point for a trend line. */
export interface TrendPoint {
  at: number;
  value: number;
}

/** A server's host figures over time, from the samples of its first target (host figures repeat per target). */
export function hostTrend(
  samples: readonly VitalsSample[],
  serverId: string,
  figure: 'loadPercent' | 'memoryPercent' | 'diskPercent',
): TrendPoint[] {
  const first = samples.find((sample) => sample.serverId === serverId)?.targetId;
  return samples
    .filter((sample) => sample.targetId === first && sample[figure] !== null)
    .map((sample) => ({ at: Date.parse(sample.sampledAt), value: sample[figure] as number }))
    .filter((point) => Number.isFinite(point.at));
}

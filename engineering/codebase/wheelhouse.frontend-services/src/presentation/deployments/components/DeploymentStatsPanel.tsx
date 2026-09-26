import { Link } from 'react-router-dom';
import { CircleCheck, LifeBuoy, Rocket, Timer } from 'lucide-react';
import { Sparkline, Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '@wow-two-beta/ui/presentation/display';
import { useDeploymentStats } from '@/application/deployments';
import { Measures } from '@/domain/common';
import { JobStatus, type DeploymentStats } from '@/domain/deployments';
import { KpiTile, LoadState, Panel, type KpiTrend } from '@/presentation/common/components';
import { Skeleton } from '@/presentation/common/skeleton';
import { JobStatusIndicator } from './JobStatusBadge';

const WINDOW_DAYS = 30;
const CHART_DAYS = 14;
const TREND_LABEL = `vs prior ${WINDOW_DAYS} days`;

// The first load's stand-in: the loaded layout over placeholder figures.
const PLACEHOLDER: DeploymentStats = {
  windowDays: WINDOW_DAYS, since: '2026-01-01', deploys: 10, succeeded: 8, failed: 1, refused: 1, pending: 0,
  successRate: 0.9, medianRolloutSeconds: 60, medianRecoverySeconds: 600, previous: null,
  daily: Array.from({ length: CHART_DAYS }, (_, day) => ({ date: `2026-01-${day + 1}`, succeeded: 1, failed: 0, refused: 0 })),
  targets: [{ targetId: 'target', deploys: 1, succeeded: 1, failed: 0, lastStatus: JobStatus.Succeeded,
    lastDeployAt: '2026-01-01T00:00:00Z', failingSince: null }],
};

/** Deployment outcomes, rollout time and recovery time over the last 30 days, each against the 30 before. */
export function DeploymentStatsPanel(props: { showTargets?: boolean }) {
  const stats = useDeploymentStats(WINDOW_DAYS);
  const showTargets = props.showTargets ?? false;
  return (
    <Panel title="Deployments · 30 days" description="From Wheelhouse's own submission records; refusals changed nothing."
      actions={<Link className="text-sm text-primary hover:underline" to="/deployments">History</Link>}>
      <LoadState loading={stats.loading} error={stats.error} empty={!stats.data} emptyTitle="No deployment records"
        onRetry={() => void stats.refetch()}
        skeleton={
          <Skeleton.Group loading label="Reading deployment records">
            <StatsBody stats={PLACEHOLDER} showTargets={showTargets} />
          </Skeleton.Group>
        }>
        {stats.data && <StatsBody stats={stats.data} showTargets={showTargets} />}
      </LoadState>
    </Panel>
  );
}

// ---- Trends ----

const signed = (value: number, text: string) => (value > 0 ? `+${text}` : value < 0 ? `−${text}` : text);

// A change is shown only when both windows have the figure.
function trend(current: number | null, previous: number | null | undefined, format: KpiTrend['format'], isInverse = false) {
  return current == null || previous == null ? null : { value: current - previous, format, isInverse, label: TREND_LABEL };
}

function StatsBody(props: { stats: DeploymentStats; showTargets: boolean }) {
  const stats = props.stats;
  const previous = stats.previous ?? null;
  const recent = stats.daily.slice(-CHART_DAYS);
  const first = recent[0]?.date;
  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <KpiTile icon={Rocket} tint="primary" label="Deploys" value={stats.deploys}
          trend={trend(stats.deploys, previous?.deploys, (value) => signed(value, String(Math.abs(value))))}
          helper={`${stats.succeeded} succeeded · ${stats.failed} failed · ${stats.refused} refused`} />
        <KpiTile icon={CircleCheck} tint="success" label="Success rate" value={Measures.percent(stats.successRate)}
          trend={trend(stats.successRate, previous?.successRate,
            (value) => signed(value, `${Math.round(Math.abs(value) * 100)} pts`))}
          helper="of finished rollouts" />
        <KpiTile icon={Timer} tint="info" label="Median rollout" value={Measures.duration(stats.medianRolloutSeconds)}
          trend={trend(stats.medianRolloutSeconds, previous?.medianRolloutSeconds,
            (value) => signed(value, Measures.duration(Math.abs(value))), true)}
          helper="start to verified" />
        <KpiTile icon={LifeBuoy} tint="warning" label="Median recovery" value={Measures.duration(stats.medianRecoverySeconds)}
          trend={trend(stats.medianRecoverySeconds, previous?.medianRecoverySeconds,
            (value) => signed(value, Measures.duration(Math.abs(value))), true)}
          helper="failure to the next success" />
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <Trend title="Finished deploys per day" data={recent.map((day) => day.succeeded + day.failed)} tone="brand" />
        <Trend title="Failed rollouts per day" data={recent.map((day) => day.failed)} tone="danger" />
      </div>
      {first && <Skeleton.Slot className="self-start text-xs text-muted-foreground">Last {CHART_DAYS} UTC days, from {first}.</Skeleton.Slot>}
      {props.showTargets && stats.targets.length > 0 && (
        <Table density="compact" isHoverable>
          <TableHead>
            <TableRow>
              <TableHeaderCell>Target</TableHeaderCell><TableHeaderCell>Deploys</TableHeaderCell>
              <TableHeaderCell>Failed</TableHeaderCell><TableHeaderCell>Last</TableHeaderCell>
              <TableHeaderCell>When</TableHeaderCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {stats.targets.map((target) => (
              <TableRow key={target.targetId}>
                <TableCell className="font-mono text-xs"><Skeleton.Slot>{target.targetId}</Skeleton.Slot></TableCell>
                <TableCell className="tabular-nums"><Skeleton.Slot>{target.deploys}</Skeleton.Slot></TableCell>
                <TableCell className="tabular-nums"><Skeleton.Slot>{target.failed}</Skeleton.Slot></TableCell>
                <TableCell>
                  <Skeleton.Slot className="flex flex-wrap items-center gap-2">
                    <JobStatusIndicator status={target.lastStatus} />
                    {target.lastRelease && <span className="font-mono text-xs">{target.lastRelease}</span>}
                  </Skeleton.Slot>
                </TableCell>
                <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                  <Skeleton.Slot title={new Date(target.lastDeployAt).toLocaleString()}>{Measures.moment(target.lastDeployAt)}</Skeleton.Slot>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}

function Trend(props: { title: string; data: number[]; tone: 'brand' | 'danger' }) {
  const total = props.data.reduce((sum, value) => sum + value, 0);
  return (
    <div className="flex flex-col gap-2 rounded-lg border border-border p-3">
      <div className="flex items-baseline justify-between text-sm">
        <span>{props.title}</span>
        <Skeleton.Slot className="font-medium tabular-nums">{total}</Skeleton.Slot>
      </div>
      <Skeleton.Slot block>
        <Sparkline data={props.data} variant="bar" tone={props.tone} height={40} min={0}
          max={Math.max(1, ...props.data)} className="w-full" ariaLabel={`${props.title}: ${props.data.join(', ')}`} />
      </Skeleton.Slot>
    </div>
  );
}

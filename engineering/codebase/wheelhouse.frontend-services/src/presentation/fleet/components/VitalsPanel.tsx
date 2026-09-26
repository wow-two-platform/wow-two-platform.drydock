import { RefreshCw } from 'lucide-react';
import { cn } from '@wow-two-beta/ui/foundation/utils';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow, Text } from '@wow-two-beta/ui/presentation/display';
import { Alert, MeterBar, StatusIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { useRefresh } from '@/application/common';
import { useFleetVitals, useServers } from '@/application/fleet';
import { Measures } from '@/domain/common';
import { FleetExtensions, USAGE_THRESHOLDS, type HostVitals, type TargetVitals } from '@/domain/fleet';
import { LoadState, Panel } from '@/presentation/common/components';
import { Skeleton } from '@/presentation/common/skeleton';
import { ConditionBadge } from '@/presentation/deployments';

/** Props for {@link VitalsPanel}. */
export interface VitalsPanelProps {
  /** Hosts only, without each target's containers. */
  compact?: boolean;
}

const GIB = 1024 ** 3;

// The first load's stand-in: real labels over placeholder values, in the loaded layout.
const PLACEHOLDER_HOST: HostVitals = {
  cpus: 4, load: [0.5, 0.5, 0.5], memoryTotalBytes: 16 * GIB, memoryAvailableBytes: 8 * GIB, uptimeSeconds: 86_400,
  disks: [{ path: '/srv/wheelhouse', totalBytes: 80 * GIB, freeBytes: 40 * GIB }],
};
const PLACEHOLDER_TARGET: TargetVitals = {
  targetId: 'target', serverId: 'host', ok: true, release: 'release', containers: [
    { service: 'service', state: 'running', health: 'healthy', restarts: 0, startedAt: null, exitCode: null,
      cpuPercent: 1, memoryBytes: GIB / 4, memoryLimitBytes: GIB },
  ],
};

/**
 * Each host's load, memory and disks and, unless compact, every target's containers. A refresh the operator asks for
 * swaps the values to skeletons and back; the minute-by-minute background read keeps them on screen.
 */
export function VitalsPanel(props: VitalsPanelProps) {
  const vitals = useFleetVitals();
  const servers = useServers();
  const { refresh, refreshing } = useRefresh(vitals.refetch);
  const groups = [...FleetExtensions.byServer(vitals.data?.targets ?? [])];

  return (
    <Panel title={props.compact ? 'Hosts' : 'Vitals'}
      description="Read over SSH every minute; nothing on the hosts changes."
      actions={
        <>
          {vitals.data && (
            <Text size="xs" color="muted">Read at {new Date(vitals.data.collectedAt).toLocaleTimeString()}</Text>
          )}
          <Button variant="ghost" tone="neutral" size="sm" aria-busy={refreshing || undefined}
            leadingSlot={<RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />}
            onClick={() => void refresh()}>
            Refresh
          </Button>
        </>
      }>
      <LoadState loading={vitals.loading} error={vitals.error} empty={!groups.length} emptyTitle="No targets to read"
        emptyDescription="Targets are defined in reviewed code (fleet.py)." onRetry={() => void vitals.refetch()}
        skeleton={
          <Skeleton.Group loading label="Reading hosts" className="flex flex-col gap-4">
            <HostSummary placeholder name="Host name" address="0.0.0.0" host={PLACEHOLDER_HOST} />
            {!props.compact && <TargetContainers placeholder target={PLACEHOLDER_TARGET} />}
          </Skeleton.Group>
        }>
        <Skeleton.Group loading={refreshing} label="Reading hosts" className="flex flex-col gap-6">
          {groups.map(([serverId, targets]) => {
            const server = servers.data?.find((item) => item.id === serverId);
            return (
              <section key={serverId} className="flex flex-col gap-4">
                <HostSummary name={server?.name ?? serverId} address={server?.host} host={FleetExtensions.hostOf(targets)} />
                {!props.compact && targets.map((target) => <TargetContainers key={target.targetId} target={target} />)}
              </section>
            );
          })}
        </Skeleton.Group>
      </LoadState>
    </Panel>
  );
}

// ---- Host ----

// Identity (host, target and service names) holds still through a refresh; only a first-load placeholder hides it.

function HostSummary(props: { name: string; address: string | undefined; host: HostVitals | null; placeholder?: boolean }) {
  const host = props.host;
  const load = host ? FleetExtensions.loadPercent(host) : null;
  const memory = host ? FleetExtensions.memoryPercent(host) : null;
  return (
    <div className="flex flex-col gap-3">
      <div className="flex h-6 items-center justify-between gap-2">
        <span className="flex min-w-0 items-baseline gap-2 font-medium">
          <Skeleton.Slot loading={!!props.placeholder} className={cn('truncate', props.placeholder && 'w-44')}>
            {props.name}
          </Skeleton.Slot>
          {props.address && (
            <Skeleton.Slot loading={!!props.placeholder}
              className={cn('font-mono text-xs text-muted-foreground', props.placeholder && 'w-20')}>
              {props.address}
            </Skeleton.Slot>
          )}
        </span>
        <Skeleton.Slot className="w-28 shrink-0 text-right text-xs text-muted-foreground">
          {host?.uptimeSeconds != null ? `up ${Measures.duration(host.uptimeSeconds)}` : 'uptime unknown'}
        </Skeleton.Slot>
      </div>
      {host ? (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          <Gauge label="CPU load" value={load}
            detail={host.load && host.cpus ? `${host.load[0].toFixed(2)} on ${host.cpus} CPUs` : 'not reported'} />
          <Gauge label="Memory" value={memory}
            detail={host.memoryTotalBytes != null
              ? `${Measures.bytes(host.memoryAvailableBytes)} free of ${Measures.bytes(host.memoryTotalBytes)}`
              : 'not reported'} />
          {host.disks.map((disk) => (
            <Gauge key={disk.path} label="Disk" qualifier={disk.path} placeholder={!!props.placeholder}
              value={FleetExtensions.diskPercent(disk)}
              detail={`${Measures.bytes(disk.freeBytes)} free of ${Measures.bytes(disk.totalBytes)}`} />
          ))}
        </div>
      ) : (
        <Text size="sm" color="muted">No readable host vitals.</Text>
      )}
    </div>
  );
}

function Gauge(props: { label: string; qualifier?: string; placeholder?: boolean; value: number | null; detail: string }) {
  const name = props.qualifier ? `${props.label} ${props.qualifier}` : props.label;
  return (
    <div className="flex flex-col gap-2 rounded-lg border border-border bg-card p-3">
      <div className="flex h-5 items-center justify-between gap-2 text-sm">
        <span className="flex min-w-0 items-center">
          <span className="shrink-0">{props.label}</span>
          {props.qualifier && (
            <Skeleton.Slot loading={!!props.placeholder} className="ml-1 truncate">{props.qualifier}</Skeleton.Slot>
          )}
        </span>
        <Skeleton.Slot className="w-12 shrink-0 text-right font-medium tabular-nums">
          {props.value == null ? '—' : `${Math.round(props.value)}%`}
        </Skeleton.Slot>
      </div>
      <Skeleton.Slot block className="h-1.5">
        <MeterBar value={props.value ?? 0} thresholds={USAGE_THRESHOLDS} size="sm" label={name} />
      </Skeleton.Slot>
      <Skeleton.Slot block className="h-4 truncate text-xs leading-4 text-muted-foreground">{props.detail}</Skeleton.Slot>
    </div>
  );
}

// ---- Containers ----

function TargetContainers(props: { target: TargetVitals; placeholder?: boolean }) {
  const target = props.target;
  return (
    <div className="flex min-w-0 flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">
        <Skeleton.Slot loading={!!props.placeholder} className="font-mono text-sm">{target.targetId}</Skeleton.Slot>
        {target.condition && <Skeleton.Slot><ConditionBadge condition={target.condition} /></Skeleton.Slot>}
        {target.release && <Skeleton.Slot className="text-xs text-muted-foreground">runs {target.release}</Skeleton.Slot>}
      </div>
      {!target.ok ? (
        <Alert severity="danger" title="Could not read this target" description={target.reason ?? 'The target did not answer.'} />
      ) : (
        <>
          {(target.problems ?? []).map((problem) => <Alert key={problem} severity="warning" description={problem} />)}
          {target.containers?.length ? (
            <div className="overflow-x-auto">
              <Table density="compact" isHoverable>
                <TableHead>
                  <TableRow>
                    <TableHeaderCell>Service</TableHeaderCell><TableHeaderCell>State</TableHeaderCell>
                    <TableHeaderCell>Restarts</TableHeaderCell><TableHeaderCell>CPU</TableHeaderCell>
                    <TableHeaderCell>Memory</TableHeaderCell><TableHeaderCell>Started</TableHeaderCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {target.containers.map((container) => (
                    <TableRow key={container.service}>
                      <TableCell className="font-mono text-xs">
                        <Skeleton.Slot loading={!!props.placeholder}>{container.service}</Skeleton.Slot>
                      </TableCell>
                      <TableCell>
                        <Skeleton.Slot>
                          <StatusIndicator tone={FleetExtensions.containerTone(container)} label={FleetExtensions.containerLabel(container)} />
                        </Skeleton.Slot>
                      </TableCell>
                      <TableCell className="tabular-nums"><Skeleton.Slot>{container.restarts}</Skeleton.Slot></TableCell>
                      <TableCell className="tabular-nums">
                        <Skeleton.Slot>{container.cpuPercent == null ? '—' : `${container.cpuPercent.toFixed(1)}%`}</Skeleton.Slot>
                      </TableCell>
                      <TableCell className="whitespace-nowrap tabular-nums">
                        <Skeleton.Slot>
                          {Measures.bytes(container.memoryBytes)}
                          {container.memoryLimitBytes != null && (
                            <span className="text-muted-foreground"> / {Measures.bytes(container.memoryLimitBytes)}</span>
                          )}
                        </Skeleton.Slot>
                      </TableCell>
                      <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                        <Skeleton.Slot title={container.startedAt ? new Date(container.startedAt).toLocaleString() : undefined}>
                          {Measures.moment(container.startedAt)}
                        </Skeleton.Slot>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ) : (
            <Text size="sm" color="muted">No containers for this target yet.</Text>
          )}
        </>
      )}
    </div>
  );
}

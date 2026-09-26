import { RotateCcw } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { Table, TableBody, TableCell, TableHead, TableHeaderCell, TableRow } from '@wow-two-beta/ui/presentation/display';
import { Measures } from '@/domain/common';
import { JobStatus, type DeploymentJob } from '@/domain/deployments';
import { TableStyles } from '@/presentation/common/components';
import { Skeleton } from '@/presentation/common/skeleton';
import { JobStatusIndicator } from './JobStatusBadge';

/** Props for {@link HistoryTable}. */
export interface HistoryTableProps {
  jobs: DeploymentJob[];
  onRedeploy?: (job: DeploymentJob) => void;
}

/** Recent deployments, newest first, with the reason any of them stopped. */
export function HistoryTable(props: HistoryTableProps) {
  return (
    <Table density="compact" isHoverable containerClassName={TableStyles.scrollBox}>
      <TableHead className={TableStyles.stickyHead}>
        <TableRow>
          <TableHeaderCell>Submitted</TableHeaderCell>
          <TableHeaderCell>Target · release</TableHeaderCell>
          <TableHeaderCell>Status</TableHeaderCell>
          <TableHeaderCell>Actor</TableHeaderCell>
          {props.onRedeploy && <TableHeaderCell><span className="sr-only">Actions</span></TableHeaderCell>}
        </TableRow>
      </TableHead>
      <TableBody>
        {props.jobs.map((job) => (
          <TableRow key={job.id}>
            <TableCell className="whitespace-nowrap text-xs text-muted-foreground"
              title={job.submittedAt ? new Date(job.submittedAt).toLocaleString() : undefined}>
              <Skeleton.Slot>{Measures.moment(job.submittedAt)}</Skeleton.Slot>
            </TableCell>
            <TableCell className="whitespace-nowrap font-mono text-xs">
              <Skeleton.Slot className="block">{job.targetId ?? '—'}</Skeleton.Slot>
              <Skeleton.Slot className="mt-0.5 text-muted-foreground">{job.release ?? job.bundleId ?? '—'}</Skeleton.Slot>
            </TableCell>
            <TableCell><Skeleton.Slot><JobStatusIndicator status={job.status} reason={job.reason} /></Skeleton.Slot></TableCell>
            <TableCell className="text-xs"><Skeleton.Slot>{job.actor ?? '—'}</Skeleton.Slot></TableCell>
            {props.onRedeploy && (
              <TableCell className="text-right">
                {job.status === JobStatus.Succeeded && job.targetId && job.bundleId && (
                  <Button variant="ghost" tone="neutral" size="sm" aria-label={`Redeploy ${job.release ?? ''}`}
                    title="Redeploy this release" leadingSlot={<RotateCcw size={14} />} onClick={() => props.onRedeploy?.(job)} />
                )}
              </TableCell>
            )}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

/** The history's first-load skeleton: placeholder rows in the table's shape. */
export function HistoryTableSkeleton(props: { rows?: number }) {
  const jobs = Array.from({ length: props.rows ?? 5 }, (_, index): DeploymentJob => ({
    id: `placeholder-${index}`, targetId: 'product-environment', release: 'release-1', actor: 'operator',
    submittedAt: '2026-01-01T00:00:00Z', status: JobStatus.Succeeded,
  }));
  return (
    <Skeleton.Group loading label="Loading deployments">
      <HistoryTable jobs={jobs} />
    </Skeleton.Group>
  );
}

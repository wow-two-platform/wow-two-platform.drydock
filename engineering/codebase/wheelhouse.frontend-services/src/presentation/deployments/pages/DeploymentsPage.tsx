import { RefreshCw, Rocket } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { useDeploymentHistory } from '@/application/deployments';
import { LoadState, PageActions, Panel } from '@/presentation/common/components';
import { DeployModal } from '../components/DeployModal';
import { DeploymentStatsPanel } from '../components/DeploymentStatsPanel';
import { HistoryTable, HistoryTableSkeleton } from '../components/HistoryTable';
import { TargetsPanel } from '../components/TargetsPanel';
import { useDeployModal } from '../hooks/useDeployModal';

/** Deploy, observe and recover releases across the code-owned targets. */
export function DeploymentsPage() {
  const history = useDeploymentHistory();
  const deploy = useDeployModal();
  return (
    <div className="flex flex-col gap-6">
      <PageActions>
        <Button variant="solid" tone="primary" leadingSlot={<Rocket size={16} />} onClick={() => deploy.openDeploy()}>
          Deploy
        </Button>
      </PageActions>
      <TargetsPanel />
      <DeploymentStatsPanel showTargets />
      <Panel title="History" description="Recent deployments with the reason any of them stopped."
        actions={<Button variant="ghost" tone="neutral" size="sm" leadingSlot={<RefreshCw size={14} />}
          onClick={() => void history.refetch()}>Refresh</Button>}>
        <LoadState loading={history.loading} error={history.error} empty={!history.data?.length}
          emptyTitle="No deployments yet" onRetry={() => void history.refetch()} skeleton={<HistoryTableSkeleton />}>
          <HistoryTable jobs={history.data ?? []}
            onRedeploy={(job) => deploy.openDeploy({ target: job.targetId ?? '', release: job.bundleId ?? '' })} />
        </LoadState>
      </Panel>
      <DeployModal key={deploy.session} open={deploy.open} onOpenChange={deploy.onOpenChange} selection={deploy.selection} />
    </div>
  );
}

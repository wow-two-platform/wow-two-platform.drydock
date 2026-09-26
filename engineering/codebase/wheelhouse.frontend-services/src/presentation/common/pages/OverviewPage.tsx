import { Link } from 'react-router-dom';
import { Rocket } from 'lucide-react';
import { Button } from '@wow-two-beta/ui/presentation/actions';
import { useDeploymentHistory } from '@/application/deployments';
import { AttentionPanel, LoadState, PageActions, Panel } from '@/presentation/common/components';
import {
  DeployModal, DeploymentStatsPanel, HistoryTable, HistoryTableSkeleton, TargetsPanel, useDeployModal,
} from '@/presentation/deployments';
import { VitalsPanel } from '@/presentation/fleet';

/** The fleet at a glance: what needs attention, host capacity, deployment health and what each target runs. */
export function OverviewPage() {
  const history = useDeploymentHistory();
  const deploy = useDeployModal();
  return (
    <div className="flex flex-col gap-6">
      <PageActions>
        <Button variant="solid" tone="primary" leadingSlot={<Rocket size={16} />} onClick={() => deploy.openDeploy()}>
          Deploy
        </Button>
      </PageActions>
      <AttentionPanel />
      <VitalsPanel compact />
      <DeploymentStatsPanel />
      <TargetsPanel />
      <Panel title="Recent deployments"
        actions={<Link className="text-sm text-primary hover:underline" to="/deployments">All deployments</Link>}>
        <LoadState loading={history.loading} error={history.error} empty={!history.data?.length}
          emptyTitle="No deployments yet" onRetry={() => void history.refetch()} skeleton={<HistoryTableSkeleton />}>
          <HistoryTable jobs={(history.data ?? []).slice(0, 5)} />
        </LoadState>
      </Panel>
      <DeployModal key={deploy.session} open={deploy.open} onOpenChange={deploy.onOpenChange} selection={deploy.selection} />
    </div>
  );
}

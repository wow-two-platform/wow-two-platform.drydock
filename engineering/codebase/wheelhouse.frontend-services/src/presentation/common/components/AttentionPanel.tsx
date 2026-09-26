import { Link } from 'react-router-dom';
import { CircleCheck } from 'lucide-react';
import { EmptyState, Text } from '@wow-two-beta/ui/presentation/display';
import { Spinner, StatusIndicator } from '@wow-two-beta/ui/presentation/feedback';
import { useDeploymentStats } from '@/application/deployments';
import { useFleetVitals } from '@/application/fleet';
import { useVaults, useVaultsHygiene } from '@/application/secrets';
import { AttentionRules } from '@/domain/overview';
import { Panel } from './Panel';

/** Everything an operator should look at across fleet, deployments and vaults, most urgent first. */
export function AttentionPanel() {
  const vitals = useFleetVitals();
  const stats = useDeploymentStats(30);
  const vaults = useVaults();
  const hygiene = useVaultsHygiene(vaults.data ?? []);
  const items = AttentionRules.sort([
    ...(vitals.data ? AttentionRules.fromVitals(vitals.data) : []),
    ...(stats.data ? AttentionRules.fromStats(stats.data) : []),
    ...(vaults.data ?? []).flatMap((vault) => AttentionRules.fromVault(vault, hygiene.byVault.get(vault.id))),
  ]);
  const unread = [
    vitals.error && 'fleet vitals',
    stats.error && 'deployment records',
    vaults.error && 'vaults',
    hygiene.errors.length > 0 && 'vault hygiene',
  ].filter((value): value is string => typeof value === 'string');
  const loading = vitals.loading || stats.loading || vaults.loading || hygiene.loading;

  return (
    <Panel title="Needs attention"
      description="Unreachable or locked targets, failing containers, full disks, failed rollouts and overdue secrets.">
      {items.length > 0 ? (
        <ul className="divide-y divide-border">
          {items.map((item) => (
            <li key={item.id} className="flex items-start justify-between gap-3 py-3">
              <StatusIndicator tone={item.tone === 'danger' ? 'destructive' : 'warning'} label={item.title}
                description={item.detail} className="min-w-0" />
              <Link className="shrink-0 text-sm text-primary hover:underline" to={item.href}>Open</Link>
            </li>
          ))}
        </ul>
      ) : loading ? (
        <div className="flex justify-center py-6"><Spinner label="Reading the fleet" /></div>
      ) : unread.length === 0 ? (
        <EmptyState size="sm" icon={<CircleCheck size={24} />} title="Nothing needs attention"
          description="Every target answered, every container is healthy, and nothing is due for rotation." />
      ) : null}
      {unread.length > 0 && (
        <Text size="xs" color="danger" className="mt-3 block">Couldn't read {unread.join(', ')}; the list may be incomplete.</Text>
      )}
    </Panel>
  );
}

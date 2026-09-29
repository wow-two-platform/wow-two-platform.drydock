import type { DeploymentJob, DeploymentTarget } from '@/domain/deployments';
import type { ContainerVitals, ServerVitals } from '@/domain/servers';

import type { WorkspaceInventory, WorkspaceProduct } from './WorkspaceInventory';

/** Represents the requested URL selection; null means absent, while an empty string remains explicit. */
export interface WorkspaceSelection {
  readonly productKey: string | null;
  readonly targetId: string | null;
  readonly inspector: string | null;
  readonly itemId: string | null;
}

/** Defines a completed inventory read or an unresolved loading/error state. */
export type WorkspaceRead<T> =
  | { readonly status: 'ready'; readonly data: T }
  | { readonly status: 'loading' }
  | { readonly status: 'error' };

/** Represents an inspector anchored to an observed target, service or submission. */
export type WorkspaceInspector =
  | { readonly kind: 'target'; readonly target: DeploymentTarget }
  | { readonly kind: 'service'; readonly target: DeploymentTarget; readonly service: ContainerVitals }
  | { readonly kind: 'deployment'; readonly target: DeploymentTarget; readonly job: DeploymentJob };

/** Defines the reads needed to resolve a requested workspace selection. */
export interface WorkspaceSelectionInput {
  readonly requested: WorkspaceSelection;
  readonly inventory: WorkspaceRead<WorkspaceInventory>;
  readonly vitals?: WorkspaceRead<ServerVitals>;
  readonly history?: WorkspaceRead<readonly DeploymentJob[]>;
}

/** Defines why an explicit selection cannot resolve within its requested scope. */
export type WorkspaceSelectionIssue =
  | 'product-not-found' | 'ambiguous-product' | 'target-not-found' | 'ambiguous-target'
  | 'target-required' | 'invalid-inspector' | 'service-not-found' | 'ambiguous-service'
  | 'deployment-not-found' | 'ambiguous-deployment' | 'deployment-target-mismatch';

/** Represents a resolved selection or the exact scope that remains unavailable. */
export type WorkspaceSelectionResult = {
  readonly requested: WorkspaceSelection;
  readonly product: WorkspaceProduct | null;
  readonly target: DeploymentTarget | null;
} & (
  | { readonly status: 'ready'; readonly selection: WorkspaceSelection; readonly inspector: WorkspaceInspector | null }
  | { readonly status: 'loading' | 'error'; readonly scope: 'inventory' | 'service' | 'deployment' }
  | { readonly status: 'unavailable'; readonly issue: WorkspaceSelectionIssue }
);

/** Represents a workspace visit without an explicit product, target or inspector. */
export const EmptyWorkspaceSelection: WorkspaceSelection = {
  productKey: null, targetId: null, inspector: null, itemId: null,
};

/** Reads literal selection identifiers without normalizing or discarding invalid explicit values. */
export function readWorkspaceSelection(search: URLSearchParams): WorkspaceSelection {
  return { productKey: search.get('product'), targetId: search.get('target'),
    inspector: search.get('inspect'), itemId: search.get('item') };
}

/** Returns updated selection parameters while preserving unrelated parameters and the original instance. */
export function writeWorkspaceSelection(search: URLSearchParams, selection: WorkspaceSelection): URLSearchParams {
  const updated = new URLSearchParams(search);
  const values = [['product', selection.productKey], ['target', selection.targetId],
    ['inspect', selection.inspector], ['item', selection.itemId]] as const;
  for (const [key, value] of values) {
    if (value === null) updated.delete(key);
    else updated.set(key, value);
  }
  return updated;
}

/** Resolves defaults only for absent parameters; pending reads and invalid explicit identifiers never switch scope. */
export function resolveWorkspaceSelection(input: WorkspaceSelectionInput): WorkspaceSelectionResult {
  const { requested, inventory } = input;
  const empty = { requested, product: null, target: null };
  if (inventory.status !== 'ready') return { ...empty, status: inventory.status, scope: 'inventory' };

  const products = requested.productKey === null
    ? inventory.data.products.slice(0, 1)
    : inventory.data.products.filter((item) => item.key === requested.productKey);
  const product = products[0] ?? null;
  if (products.length > 1) return { ...empty, status: 'unavailable', issue: 'ambiguous-product' };
  if (!product && requested.productKey !== null)
    return { ...empty, status: 'unavailable', issue: 'product-not-found' };

  const targets = requested.targetId === null
    ? product?.targets.slice(0, 1) ?? []
    : product?.targets.filter((item) => item.id === requested.targetId) ?? [];
  const target = targets[0] ?? null;
  const context = { requested, product, target };
  if (targets.length > 1) return { ...context, target: null, status: 'unavailable', issue: 'ambiguous-target' };
  if (!target && requested.targetId !== null)
    return { ...context, status: 'unavailable', issue: 'target-not-found' };

  const selection = { ...requested, productKey: product?.key ?? null, targetId: target?.id ?? null };
  if (requested.inspector === null && requested.itemId === null)
    return { ...context, status: 'ready', selection, inspector: target ? { kind: 'target', target } : null };
  if (requested.inspector !== 'target' && requested.inspector !== 'service' && requested.inspector !== 'deployment')
    return { ...context, status: 'unavailable', issue: 'invalid-inspector' };
  if (!target) return { ...context, status: 'unavailable', issue: 'target-required' };
  if (requested.inspector === 'target') {
    if (requested.itemId !== null) return { ...context, status: 'unavailable', issue: 'invalid-inspector' };
    return { ...context, status: 'ready', selection, inspector: { kind: 'target', target } };
  }
  if (requested.itemId === null || requested.itemId === '')
    return { ...context, status: 'unavailable', issue: 'invalid-inspector' };

  if (requested.inspector === 'service') {
    const read = input.vitals ?? { status: 'loading' };
    if (read.status !== 'ready') return { ...context, status: read.status, scope: 'service' };
    const readings = read.data.targets.filter((item) => item.targetId === target.id);
    const reading = readings[0];
    if (readings.length !== 1 || !reading?.ok || reading.containers == null)
      return { ...context, status: 'error', scope: 'service' };
    const services = reading.containers.filter((item) => item.service === requested.itemId);
    const service = services[0];
    if (!service) return { ...context, status: 'unavailable', issue: 'service-not-found' };
    if (services.length !== 1) return { ...context, status: 'unavailable', issue: 'ambiguous-service' };
    return { ...context, status: 'ready', selection, inspector: { kind: 'service', target, service } };
  }

  const read = input.history ?? { status: 'loading' };
  if (read.status !== 'ready') return { ...context, status: read.status, scope: 'deployment' };
  const jobs = read.data.filter((item) => item.id === requested.itemId);
  const job = jobs[0];
  if (!job) return { ...context, status: 'unavailable', issue: 'deployment-not-found' };
  if (jobs.length !== 1) return { ...context, status: 'unavailable', issue: 'ambiguous-deployment' };
  if (job.targetId !== target.id)
    return { ...context, status: 'unavailable', issue: 'deployment-target-mismatch' };
  return { ...context, status: 'ready', selection, inspector: { kind: 'deployment', target, job } };
}

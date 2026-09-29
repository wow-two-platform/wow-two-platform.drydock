import type { DeploymentTarget } from '@/domain/deployments';
import type { Product } from '@/domain/products';

/** Defines a reviewed association between a registered product and the runner catalog. */
export interface WorkspaceProductBinding {
  readonly registrySlug: string;
  readonly repository: string;
  readonly runnerProduct: string;
}

/** Represents one registered or runner-only product in the workspace navigator. */
export interface WorkspaceProduct {
  readonly key: string;
  readonly registry: Product | null;
  readonly runnerProduct: string | null;
  readonly targets: readonly DeploymentTarget[];
  readonly binding: 'mapped' | 'unconfigured' | 'unregistered' | 'conflict';
  readonly issue: 'duplicate-binding' | 'repository-mismatch' | 'ambiguous-registry' | null;
}

/** Represents the complete navigator inventory, including unmatched runner products. */
export interface WorkspaceInventory {
  readonly products: readonly WorkspaceProduct[];
}

/** Maps reviewed registry identities to literal products in the runner's artifact catalog. */
export const WorkspaceProductBindings: readonly WorkspaceProductBinding[] = [{
  registrySlug: 'forever-pin',
  repository: 'sulton-max/10x-venture-forever-pin',
  runnerProduct: 'foreverpin',
}];

/** Builds from successfully loaded inventories; callers must preserve loading and errors separately. */
export function buildWorkspaceInventory(
  products: readonly Product[],
  targets: readonly DeploymentTarget[],
  bindings: readonly WorkspaceProductBinding[] = WorkspaceProductBindings,
): WorkspaceInventory {
  const groups = new Map<string, DeploymentTarget[]>();
  for (const target of targets) {
    const group = groups.get(target.product) ?? [];
    group.push(target);
    groups.set(target.product, group);
  }

  const entries = products.map((product): WorkspaceProduct => {
    const candidates = bindings.filter((binding) => binding.registrySlug === product.slug);
    const entry = {
      key: `registry:${product.id}`,
      registry: product,
      runnerProduct: null,
      targets: [],
    } as const;
    const binding = candidates[0];
    if (!binding) return { ...entry, binding: 'unconfigured', issue: null };
    if (candidates.length !== 1 || bindings.filter((item) => item.runnerProduct === binding.runnerProduct).length !== 1)
      return { ...entry, binding: 'conflict', issue: 'duplicate-binding' };
    if (!sameRepository(product.repo, binding.repository))
      return { ...entry, binding: 'conflict', issue: 'repository-mismatch' };
    const matches = products.filter((item) => item.slug === binding.registrySlug && sameRepository(item.repo, binding.repository));
    if (matches.length !== 1)
      return { ...entry, binding: 'conflict', issue: 'ambiguous-registry' };
    return { ...entry, runnerProduct: binding.runnerProduct, targets: groups.get(binding.runnerProduct) ?? [],
      binding: 'mapped', issue: null };
  });

  const mapped = new Set(entries.flatMap((entry) => entry.runnerProduct === null ? [] : [entry.runnerProduct]));
  for (const [product, productTargets] of groups) {
    if (!mapped.has(product))
      entries.push({ key: `runner:${product}`, registry: null, runnerProduct: product,
        targets: productTargets, binding: 'unregistered', issue: null });
  }
  return { products: entries };
}

function sameRepository(left: string, right: string): boolean {
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

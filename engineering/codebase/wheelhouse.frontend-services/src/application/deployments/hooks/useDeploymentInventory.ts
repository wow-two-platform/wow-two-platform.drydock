import { toValue, type MaybeRefOrGetter } from "vue";

import { useAppQuery } from "@/bootstrap/query";
import { deploymentsApi } from "@/integration/deployments";

import { DeploymentKeys } from "../DeploymentKeys";

/** Recent deployments with their last observed outcome. */
export function useDeploymentHistory() {
  return useAppQuery({
    key: DeploymentKeys.history,
    queryFn: ({ signal }) => deploymentsApi.listHistory(signal),
  });
}

/** Deployment outcomes, rollout time and recovery time over the last `days` UTC days. */
export function useDeploymentStats(days: MaybeRefOrGetter<number> = 30) {
  return useAppQuery({
    key: () => DeploymentKeys.stats(toValue(days)),
    queryFn: ({ signal }) => deploymentsApi.getStats(toValue(days), signal),
  });
}

/** The code-owned deployment targets. */
export function useDeploymentTargets() {
  return useAppQuery({
    key: DeploymentKeys.targets,
    queryFn: ({ signal }) => deploymentsApi.listTargets(signal),
  });
}

/** Published release bundles from approved sources. */
export function useReleaseArtifacts() {
  return useAppQuery({
    key: DeploymentKeys.releases,
    queryFn: ({ signal }) => deploymentsApi.listReleases(signal),
  });
}

/** What one target runs and whether it accepts a deployment. */
export function useTargetState(target: MaybeRefOrGetter<string | null>) {
  return useAppQuery({
    key: () => DeploymentKeys.state(toValue(target) ?? ""),
    queryFn: ({ signal }) =>
      deploymentsApi.getTargetState(toValue(target) ?? "", signal),
    enabled: () => Boolean(toValue(target)),
    meta: { suppressGlobalError: true },
  });
}

/** A product's branches, for choosing a commit to build or deploy to dev. */
export function useProductBranches(product: MaybeRefOrGetter<string | null>) {
  return useAppQuery({
    key: () => DeploymentKeys.branches(toValue(product) ?? ""),
    queryFn: ({ signal }) =>
      deploymentsApi.listBranches(toValue(product) ?? "", signal),
    enabled: () => Boolean(toValue(product)),
    meta: { suppressGlobalError: true },
  });
}

/** A branch's recent commits, each with its build when one exists. */
export function useProductCommits(
  product: MaybeRefOrGetter<string | null>,
  branch: MaybeRefOrGetter<string | null>,
) {
  return useAppQuery({
    key: () =>
      DeploymentKeys.commits(toValue(product) ?? "", toValue(branch) ?? ""),
    queryFn: ({ signal }) =>
      deploymentsApi.listCommits(
        toValue(product) ?? "",
        toValue(branch) ?? "",
        signal,
      ),
    enabled: () => Boolean(toValue(product) && toValue(branch)),
    meta: { suppressGlobalError: true },
  });
}

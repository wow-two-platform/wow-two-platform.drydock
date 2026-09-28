/** Query keys for deployment reads; writes invalidate through these. */
export const DeploymentKeys = {
  all: ["deployments"] as const,
  history: ["deployments", "history"] as const,
  targets: ["deployments", "targets"] as const,
  releases: ["deployments", "releases"] as const,
  stats: (days: number) => ["deployments", "stats", days] as const,
  statsAll: ["deployments", "stats"] as const,
  state: (target: string) => ["deployments", "state", target] as const,
  outcome: (id: string) => ["deployments", "outcome", id] as const,
  branches: (product: string) => ["deployments", "branches", product] as const,
  commits: (product: string, branch: string) =>
    ["deployments", "commits", product, branch] as const,
  commitsAll: ["deployments", "commits"] as const,
  logs: (target: string, service: string, tail: number) =>
    ["deployments", "logs", target, service, tail] as const,
};

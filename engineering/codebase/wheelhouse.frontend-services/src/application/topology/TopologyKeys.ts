/** Target-scoped declarations invalidated when a rollout changes the verified release. */
export const TopologyKeys = {
  target: (target: string) => ["deployments", "topology", target] as const,
};

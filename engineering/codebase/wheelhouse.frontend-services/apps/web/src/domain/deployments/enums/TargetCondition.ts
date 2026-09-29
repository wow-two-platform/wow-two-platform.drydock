/** Whether a target accepts a deployment. */
export const TargetCondition = {
  Ready: 'ready',
  Running: 'running',
  NeedsReconciliation: 'needs_reconciliation',
} as const;
export type TargetCondition = (typeof TargetCondition)[keyof typeof TargetCondition];

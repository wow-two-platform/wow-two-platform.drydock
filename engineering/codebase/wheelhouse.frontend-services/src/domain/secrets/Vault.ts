/** Whether Wheelhouse can administer a vault right now. */
export const VaultStatus = {
  Unsealed: 'unsealed',
  Sealed: 'sealed',
  Unreachable: 'unreachable',
} as const;
export type VaultStatus = (typeof VaultStatus)[keyof typeof VaultStatus];

/** A code-owned vault; its endpoint stays on the server. */
export interface VaultSummary {
  id: string;
  name: string;
  serverId: string;
  status: VaultStatus;
}

/** A vault namespace, typically one product environment. */
export interface VaultNamespace {
  slug: string;
  name: string;
  createdAtUtc: string;
}

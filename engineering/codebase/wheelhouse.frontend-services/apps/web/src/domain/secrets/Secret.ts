/** Whether the vault serves a secret to products. */
export const SecretState = {
  Active: 'active',
  Disabled: 'disabled',
} as const;
export type SecretState = (typeof SecretState)[keyof typeof SecretState];

/** A secret's metadata; its value is never readable from Wheelhouse. */
export interface SecretMetadata {
  namespace: string;
  key: string;
  state: SecretState;
  version: number;
  description?: string | null;
  updatedAtUtc: string;
}

/** A namespace-scoped product token's metadata. */
export interface VaultToken {
  id: string;
  name: string;
  createdAtUtc: string;
  expiresAtUtc: string | null;
  isRevoked: boolean;
}

/** A freshly minted token; the only moment its value exists outside the product. */
export interface MintedToken {
  id: string;
  name: string;
  namespace: string;
  token: string;
}

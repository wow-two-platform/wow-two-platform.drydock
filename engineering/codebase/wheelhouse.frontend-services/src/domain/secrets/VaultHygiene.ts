/** An active secret whose value is older than the rotation threshold. */
export interface OverdueSecret {
  namespace: string;
  key: string;
  updatedAtUtc: string;
  ageDays: number;
}

/** A product token that expired, expires soon, or is older than the rotation threshold. */
export interface OverdueToken {
  namespace: string;
  id: string;
  name: string;
  createdAtUtc: string;
  ageDays: number;
  expiresAtUtc: string | null;
  reason: 'expired' | 'expires soon' | 'rotation due';
}

/** A vault's rotation hygiene across every namespace; metadata only. */
export interface VaultHygiene {
  vault: string;
  namespaces: number;
  secrets: number;
  disabledSecrets: number;
  tokens: number;
  overdueSecrets: OverdueSecret[];
  overdueTokens: OverdueToken[];
  secretRotationDays: number;
  tokenRotationDays: number;
}

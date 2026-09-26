import { useAppQueries } from '@wow-two-beta/ui/query';
import { VaultStatus, type VaultHygiene, type VaultSummary } from '@/domain/secrets';
import { secretsApi } from '@/integration/secrets';
import { SecretKeys } from './SecretKeys';

/** Rotation hygiene for every unsealed vault; shares its cache with {@link useVaultHygiene}. */
export function useVaultsHygiene(vaults: readonly VaultSummary[]) {
  const ready = vaults.filter((vault) => vault.status === VaultStatus.Unsealed);
  const hygiene = useAppQueries({
    queries: ready.map((vault) => ({
      key: SecretKeys.hygiene(vault.id),
      queryFn: ({ signal }: { signal: AbortSignal }) => secretsApi.getHygiene(vault.id, signal),
    })),
  });
  const byVault = new Map<string, VaultHygiene>();
  ready.forEach((vault, index) => {
    const data = hygiene.data[index];
    if (data) byVault.set(vault.id, data);
  });
  return { loading: hygiene.loading, errors: hygiene.errors, byVault };
}

import { computed, toValue, type MaybeRefOrGetter } from "vue";
import { useAppQueries } from "@/bootstrap/query";
import {
  VaultStatus,
  type VaultHygiene,
  type VaultSummary,
} from "@/domain/secrets";
import { secretsApi } from "@/integration/secrets";
import { SecretKeys } from "./SecretKeys";

/** Shares metadata hygiene queries across the current set of unsealed vaults. */
export function useVaultsHygiene(
  vaults: MaybeRefOrGetter<readonly VaultSummary[]>,
) {
  const ready = computed(() =>
    toValue(vaults).filter((vault) => vault.status === VaultStatus.Unsealed),
  );
  const hygiene = useAppQueries({
    queries: () =>
      ready.value.map((vault) => ({
        key: SecretKeys.hygiene(vault.id),
        queryFn: ({ signal }: { signal: AbortSignal }) =>
          secretsApi.getHygiene(vault.id, signal),
      })),
  });
  const byVault = computed(() => {
    const result = new Map<string, VaultHygiene>();
    ready.value.forEach((vault, index) => {
      const data = hygiene.data.value[index];
      if (data) result.set(vault.id, data);
    });
    return result;
  });
  return { loading: hygiene.loading, errors: hygiene.errors, byVault };
}

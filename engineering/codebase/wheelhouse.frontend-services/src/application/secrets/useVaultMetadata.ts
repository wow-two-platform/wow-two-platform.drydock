import { useAppQuery } from '@wow-two-beta/ui/query';
import { secretsApi } from '@/integration/secrets';
import { SecretKeys } from './SecretKeys';

/** The configured vaults with their sealed state. */
export function useVaults() {
  return useAppQuery({ key: SecretKeys.vaults, queryFn: ({ signal }) => secretsApi.listVaults(signal) });
}

/** Which of a vault's secrets and product tokens are due for rotation. */
export function useVaultHygiene(vault: string) {
  return useAppQuery({
    key: SecretKeys.hygiene(vault),
    queryFn: ({ signal }) => secretsApi.getHygiene(vault, signal),
    enabled: vault !== '',
    meta: { suppressGlobalError: true },
  });
}

/** One vault's namespaces. */
export function useVaultNamespaces(vault: string) {
  return useAppQuery({
    key: SecretKeys.namespaces(vault),
    queryFn: ({ signal }) => secretsApi.listNamespaces(vault, signal),
    enabled: vault !== '',
  });
}

/** One namespace's secret metadata. */
export function useVaultSecrets(vault: string, ns: string) {
  return useAppQuery({
    key: SecretKeys.secrets(vault, ns),
    queryFn: ({ signal }) => secretsApi.listSecrets(vault, ns, signal),
    enabled: vault !== '' && ns !== '',
  });
}

/** One namespace's product tokens. */
export function useVaultTokens(vault: string, ns: string) {
  return useAppQuery({
    key: SecretKeys.tokens(vault, ns),
    queryFn: ({ signal }) => secretsApi.listTokens(vault, ns, signal),
    enabled: vault !== '' && ns !== '',
  });
}

import { toValue, type MaybeRefOrGetter } from "vue";
import { useAppQuery } from "@/bootstrap/query";
import { secretsApi } from "@/integration/secrets";
import { SecretKeys } from "./SecretKeys";

/** Loads the configured vaults and their sealed state. */
export function useVaults() {
  return useAppQuery({
    key: SecretKeys.vaults,
    queryFn: ({ signal }) => secretsApi.listVaults(signal),
  });
}

/** Loads rotation hygiene for the selected unsealed vault. */
export function useVaultHygiene(vault: MaybeRefOrGetter<string>) {
  return useAppQuery({
    key: () => SecretKeys.hygiene(toValue(vault)),
    queryFn: ({ signal }) => secretsApi.getHygiene(toValue(vault), signal),
    enabled: () => toValue(vault) !== "",
    meta: { suppressGlobalError: true },
  });
}

/** Loads namespaces under the current vault selection. */
export function useVaultNamespaces(vault: MaybeRefOrGetter<string>) {
  return useAppQuery({
    key: () => SecretKeys.namespaces(toValue(vault)),
    queryFn: ({ signal }) => secretsApi.listNamespaces(toValue(vault), signal),
    enabled: () => toValue(vault) !== "",
  });
}

/** Loads secret metadata only; plaintext is never readable from this query. */
export function useVaultSecrets(
  vault: MaybeRefOrGetter<string>,
  ns: MaybeRefOrGetter<string>,
) {
  return useAppQuery({
    key: () => SecretKeys.secrets(toValue(vault), toValue(ns)),
    queryFn: ({ signal }) =>
      secretsApi.listSecrets(toValue(vault), toValue(ns), signal),
    enabled: () => toValue(vault) !== "" && toValue(ns) !== "",
  });
}

/** Loads token metadata only; minted plaintext is never cached here. */
export function useVaultTokens(
  vault: MaybeRefOrGetter<string>,
  ns: MaybeRefOrGetter<string>,
) {
  return useAppQuery({
    key: () => SecretKeys.tokens(toValue(vault), toValue(ns)),
    queryFn: ({ signal }) =>
      secretsApi.listTokens(toValue(vault), toValue(ns), signal),
    enabled: () => toValue(vault) !== "" && toValue(ns) !== "",
  });
}

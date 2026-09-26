import { useCallback, useState } from 'react';
import type { QueryKey } from '@tanstack/react-query';
import { toApiError, useQueryCache } from '@wow-two-beta/ui/query';
import type { ApiError } from '@/integration/common';
import { secretsApi } from '@/integration/secrets';
import { SecretKeys } from './SecretKeys';

// Vault writes skip the mutation cache on purpose: secret values and minted tokens must not linger in it.
function useVaultChange() {
  const cache = useQueryCache();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);

  const run = useCallback(async <T,>(operation: () => Promise<T>, invalidates: readonly QueryKey[]): Promise<T | null> => {
    setLoading(true);
    setError(null);
    try {
      const result = await operation();
      await Promise.all(invalidates.map((key) => cache.invalidate(key)));
      return result;
    } catch (cause) {
      setError(toApiError(cause));
      return null;
    } finally {
      setLoading(false);
    }
  }, [cache]);

  return { run, loading, error, reset: useCallback(() => setError(null), []) };
}

/** Creates namespaces in one vault. */
export function useNamespaceCreate(vault: string) {
  const change = useVaultChange();
  return {
    ...change,
    create: (slug: string, name: string) =>
      change.run(() => secretsApi.createNamespace(vault, slug, name), [SecretKeys.namespaces(vault)]),
  };
}

/** Writes, disables and re-enables secrets in one namespace. */
export function useSecretChanges(vault: string, ns: string) {
  const change = useVaultChange();
  return {
    ...change,
    set: (key: string, value: string, description?: string) =>
      change.run(() => secretsApi.setSecret(vault, ns, key, value, description), [SecretKeys.secrets(vault, ns), SecretKeys.hygiene(vault)]),
    setDisabled: (key: string, disabled: boolean) =>
      change.run(() => secretsApi.setSecretState(vault, ns, key, disabled), [SecretKeys.secrets(vault, ns), SecretKeys.hygiene(vault)]),
  };
}

/** Mints and revokes product tokens in one namespace. */
export function useTokenChanges(vault: string, ns: string) {
  const change = useVaultChange();
  return {
    ...change,
    mint: (name: string) =>
      change.run(() => secretsApi.mintToken(vault, ns, name), [SecretKeys.tokens(vault, ns), SecretKeys.hygiene(vault)]),
    revoke: (id: string) =>
      change.run(() => secretsApi.revokeToken(vault, ns, id), [SecretKeys.tokens(vault, ns), SecretKeys.hygiene(vault)]),
  };
}

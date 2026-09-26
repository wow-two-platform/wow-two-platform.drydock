import type { MintedToken, SecretMetadata, VaultHygiene, VaultNamespace, VaultSummary, VaultToken } from '@/domain/secrets';
import { requestData } from '@/integration/common';

const segment = encodeURIComponent;
const base = (vault: string) => `/api/vaults/${segment(vault)}`;

/** Vault administration through Wheelhouse; every write carries the vault action header. */
export const secretsApi = {
  listVaults: (signal?: AbortSignal) => requestData<VaultSummary[]>('/api/vaults', { signal }),

  getHygiene: (vault: string, signal?: AbortSignal) => requestData<VaultHygiene>(`${base(vault)}/hygiene`, { signal }),

  listNamespaces: (vault: string, signal?: AbortSignal) =>
    requestData<VaultNamespace[]>(`${base(vault)}/namespaces`, { signal }),

  createNamespace: (vault: string, slug: string, name: string) =>
    requestData<unknown>(`${base(vault)}/namespaces`, { method: 'POST', action: 'vault', body: JSON.stringify({ slug, name }) }),

  listSecrets: (vault: string, ns: string, signal?: AbortSignal) =>
    requestData<SecretMetadata[]>(`${base(vault)}/secrets?ns=${segment(ns)}`, { signal }),

  setSecret: (vault: string, ns: string, key: string, value: string, description?: string) =>
    requestData<SecretMetadata>(`${base(vault)}/secrets/${segment(ns)}/${segment(key)}`, {
      method: 'PUT',
      action: 'vault',
      body: JSON.stringify({ value, ...(description ? { description } : {}) }),
    }),

  setSecretState: (vault: string, ns: string, key: string, disabled: boolean) =>
    requestData<unknown>(`${base(vault)}/secrets/${segment(ns)}/${segment(key)}/state`, {
      method: 'POST',
      action: 'vault',
      body: JSON.stringify({ disabled }),
    }),

  listTokens: (vault: string, ns: string, signal?: AbortSignal) =>
    requestData<VaultToken[]>(`${base(vault)}/namespaces/${segment(ns)}/tokens`, { signal }),

  mintToken: (vault: string, ns: string, name: string) =>
    requestData<MintedToken>(`${base(vault)}/namespaces/${segment(ns)}/tokens`, {
      method: 'POST',
      action: 'vault',
      body: JSON.stringify({ name }),
    }),

  revokeToken: (vault: string, ns: string, id: string) =>
    requestData<unknown>(`${base(vault)}/namespaces/${segment(ns)}/tokens/${segment(id)}/revoke`, {
      method: 'POST',
      action: 'vault',
    }),
};

import type {
  MintedToken,
  SecretMetadata,
  VaultHygiene,
  VaultNamespace,
  VaultSummary,
  VaultToken,
} from "@/domain/secrets";
import { requestData } from "@/integration/common";
import {
  AcknowledgementSchema,
  MintedTokenSchema,
  SecretMetadataSchema,
  VaultHygieneSchema,
  VaultNamespaceSchema,
  VaultSummarySchema,
  VaultTokenSchema,
} from "./schemas";

const segment = encodeURIComponent;
const base = (vault: string) => `/api/vaults/${segment(vault)}`;

/** Vault administration through Wheelhouse; every write carries the vault action header. */
export const secretsApi = {
  listVaults: (signal?: AbortSignal) =>
    requestData<VaultSummary[]>("/api/vaults", VaultSummarySchema.array(), {
      signal,
    }),

  getHygiene: (vault: string, signal?: AbortSignal) =>
    requestData<VaultHygiene>(`${base(vault)}/hygiene`, VaultHygieneSchema, {
      signal,
    }),

  listNamespaces: (vault: string, signal?: AbortSignal) =>
    requestData<VaultNamespace[]>(
      `${base(vault)}/namespaces`,
      VaultNamespaceSchema.array(),
      { signal },
    ),

  createNamespace: (
    vault: string,
    slug: string,
    name: string,
    signal?: AbortSignal,
  ) =>
    requestData<unknown>(`${base(vault)}/namespaces`, AcknowledgementSchema, {
      signal,
      method: "POST",
      action: "vault",
      body: { slug, name },
    }),

  listSecrets: (vault: string, ns: string, signal?: AbortSignal) =>
    requestData<SecretMetadata[]>(
      `${base(vault)}/secrets?ns=${segment(ns)}`,
      SecretMetadataSchema.array(),
      { signal },
    ),

  setSecret: (
    vault: string,
    ns: string,
    key: string,
    value: string,
    description?: string,
    signal?: AbortSignal,
  ) =>
    requestData<SecretMetadata>(
      `${base(vault)}/secrets/${segment(ns)}/${segment(key)}`,
      SecretMetadataSchema,
      {
        signal,
        method: "PUT",
        action: "vault",
        body: { value, ...(description ? { description } : {}) },
      },
    ),

  setSecretState: (
    vault: string,
    ns: string,
    key: string,
    disabled: boolean,
    signal?: AbortSignal,
  ) =>
    requestData<unknown>(
      `${base(vault)}/secrets/${segment(ns)}/${segment(key)}/state`,
      AcknowledgementSchema,
      {
        signal,
        method: "POST",
        action: "vault",
        body: { disabled },
      },
    ),

  listTokens: (vault: string, ns: string, signal?: AbortSignal) =>
    requestData<VaultToken[]>(
      `${base(vault)}/namespaces/${segment(ns)}/tokens`,
      VaultTokenSchema.array(),
      { signal },
    ),

  mintToken: (vault: string, ns: string, name: string, signal?: AbortSignal) =>
    requestData<MintedToken>(
      `${base(vault)}/namespaces/${segment(ns)}/tokens`,
      MintedTokenSchema,
      {
        signal,
        method: "POST",
        action: "vault",
        body: { name },
      },
    ),

  revokeToken: (vault: string, ns: string, id: string, signal?: AbortSignal) =>
    requestData<unknown>(
      `${base(vault)}/namespaces/${segment(ns)}/tokens/${segment(id)}/revoke`,
      AcknowledgementSchema,
      {
        signal,
        method: "POST",
        action: "vault",
      },
    ),
};

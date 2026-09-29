import { z } from "zod";

/** Editable namespace identity before its request is validated. */
export interface NamespaceFormModel {
  slug: string;
  name: string;
}

/** Local-only secret editor; its value must never enter query or mutation storage. */
export interface SecretFormModel {
  key: string;
  value: string;
  description: string;
}

/** Editable token name; minted plaintext belongs only to the reveal dialog. */
export interface TokenFormModel {
  name: string;
}

/** Namespace input parsing and display-safe validation. */
export const namespaceFormSchema = z.object({
  slug: z.string().trim().min(1, "Enter a namespace slug."),
  name: z.string().trim().min(1, "Enter a namespace name."),
});

/** Secret values preserve whitespace exactly as supplied. */
export const secretFormSchema = z.object({
  key: z.string().trim().min(1, "Enter a secret key."),
  value: z.string().min(1, "Enter a secret value."),
  description: z.string(),
});

/** Token names are required before minting. */
export const tokenFormSchema = z.object({
  name: z.string().trim().min(1, "Enter a token name."),
});

/** Creates detached blank namespace editing state. */
export function createNamespaceForm(): NamespaceFormModel {
  return { slug: "", name: "" };
}

/** Creates detached secret editing state, preserving a rotation's immutable key. */
export function createSecretForm(key = ""): SecretFormModel {
  return { key, value: "", description: "" };
}

/** Creates detached blank token editing state. */
export function createTokenForm(): TokenFormModel {
  return { name: "" };
}

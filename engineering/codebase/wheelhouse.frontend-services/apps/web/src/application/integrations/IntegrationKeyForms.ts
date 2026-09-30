import { z } from "zod";

/** Represents the editable fields of a new integration key. */
export interface IntegrationKeyFormModel {
  name: string;
}

/** Validates a key name the way the host does; the name reaches the audit trail as `key:{name}`. */
export const integrationKeyFormSchema = z.object({
  name: z
    .string()
    .trim()
    .regex(/^[A-Za-z0-9][A-Za-z0-9 ._-]{0,59}$/, "Use 1–60 letters, digits, spaces, dots, dashes or underscores."),
});

/** Creates detached blank key-creation state. */
export function createIntegrationKeyForm(): IntegrationKeyFormModel {
  return { name: "" };
}

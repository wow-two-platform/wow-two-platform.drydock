/** Query keys for audit reads; every operator action invalidates them. */
export const AuditKeys = {
  all: ["audit"] as const,
  list: (limit: number, before: number | null) => ["audit", "list", limit, before] as const,
  verification: ["audit", "verification"] as const,
};

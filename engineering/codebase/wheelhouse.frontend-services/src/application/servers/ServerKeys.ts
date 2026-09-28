/** Query keys for server reads. */
export const ServerKeys = {
  servers: ["servers", "catalog"] as const,
  vitals: ["servers", "vitals"] as const,
  history: (hours: number) => ["servers", "history", hours] as const,
};

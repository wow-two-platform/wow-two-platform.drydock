/** A code-owned product environment on one fleet host. */
export interface DeploymentTarget {
  id: string;
  product: string;
  environment: string;
  serverId: string;
  provider: string;
  host: string;
  /** Dev takes a build of any commit or branch; test and prod take published releases only. */
  acceptsCandidates: boolean;
  /** Prod on the local server deploys only after the operator types the target ID. */
  needsConfirmation: boolean;
}

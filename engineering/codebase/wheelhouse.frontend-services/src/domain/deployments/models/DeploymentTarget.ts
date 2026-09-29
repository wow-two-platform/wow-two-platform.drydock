/** A code-owned product environment on one server. */
export interface DeploymentTarget {
  id: string;
  product: string;
  environment: string;
  serverId: string;
  provider: string;
  host: string;
  /** Dev takes a build of any commit or branch; test and prod take published releases. */
  acceptsCandidates: boolean;
  /** Test also takes a build of the product's `test` branch. */
  acceptsTestBuilds: boolean;
  /** Prod on the local server deploys only after the operator types the target ID. */
  needsConfirmation: boolean;
  /** Prod takes a release only after it succeeded on test; typing the target ID skips the pass. */
  requiresTestPass: boolean;
}

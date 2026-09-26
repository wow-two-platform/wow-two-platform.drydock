/** A code-owned product environment on one fleet host. */
export interface DeploymentTarget {
  id: string;
  product: string;
  environment: string;
  serverId: string;
  provider: string;
  host: string;
}

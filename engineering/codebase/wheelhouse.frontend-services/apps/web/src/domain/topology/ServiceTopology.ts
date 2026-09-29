/** Availability of the Compose declaration saved with the target's verified release. */
export const TopologyAvailability = {
  Available: "available",
  NotDeployed: "not-deployed",
  Unavailable: "unavailable",
} as const;
export type TopologyAvailability =
  (typeof TopologyAvailability)[keyof typeof TopologyAvailability];

/** A site a service serves through the ingress, with what the verified rollout recorded about it. */
export interface TopologySite {
  readonly name: string;
  /** The path prefix the site routes to this service; `/` for the whole host. */
  readonly path: string;
  readonly port: number;
  readonly exposure: "public" | "private";
  /** The site's address; null when the target publishes no host for it. */
  readonly url: string | null;
  /** Whether the site answered through the ingress after the rollout; null when it was not probed. */
  readonly reachable: boolean | null;
}

/** The release in which a service last changed; an older version means no change since. */
export interface TopologyVersion {
  readonly version: string;
  readonly changedIn: string;
}

/** Host platform services a release may declare it needs. */
export type PlatformService = "postgres" | "valkey" | "broker";

/** One declared Compose service, with only safe operational metadata. */
export interface TopologyService {
  readonly name: string;
  readonly image: string | null;
  readonly networks: readonly string[];
  readonly volumes: readonly string[];
  readonly ports: readonly string[];
  readonly version: TopologyVersion | null;
  readonly needs: readonly PlatformService[];
  readonly sites: readonly TopologySite[];
}

/** A logical Compose network; external resource names are deliberately absent. */
export interface TopologyNetwork {
  readonly name: string;
  readonly external: boolean;
}

/** A named Compose volume, excluding bind paths and mount destinations. */
export interface TopologyVolume {
  readonly name: string;
  readonly external: boolean;
}

/** A declared startup dependency: `from` waits for `to`, without implying application traffic. */
export interface TopologyDependency {
  readonly from: string;
  readonly to: string;
  readonly condition: string | null;
  readonly required: boolean;
}

/** The safe Compose projection belonging to one target's verified release. */
export interface ServiceTopology {
  readonly targetId: string;
  readonly availability: TopologyAvailability;
  readonly release: string | null;
  readonly collectedAt: string;
  readonly services: readonly TopologyService[];
  readonly networks: readonly TopologyNetwork[];
  readonly volumes: readonly TopologyVolume[];
  readonly dependencies: readonly TopologyDependency[];
  readonly warnings: readonly string[];
}

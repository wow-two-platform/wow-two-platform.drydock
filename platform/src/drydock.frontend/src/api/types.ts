/** Connectivity state of a registered server (mirrors the backend enum, serialized as a string). */
export type ServerStatus = 'Unknown' | 'Reachable' | 'Unreachable' | 'Provisioning';

/** A registered deploy-target server. */
export interface ServerDto {
  id: string;
  name: string;
  host: string;
  sshUser: string;
  region: string | null;
  status: ServerStatus;
  createdAtUtc: string;
}

/** Body for registering a server. */
export interface RegisterServerRequest {
  name: string;
  host: string;
  sshUser: string;
  sshPort: number;
  region: string | null;
}

/** System liveness payload from /api/system/status. */
export interface SystemStatus {
  service: string;
  status: string;
}

/** RFC 7807 problem detail returned on errors. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

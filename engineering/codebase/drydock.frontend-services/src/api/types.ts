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

/** Lifecycle state of a portfolio product (mirrors the backend enum, serialized as a string). */
export type ProductStatus = 'Draft' | 'Active' | 'Paused' | 'Killed';

/** A registered portfolio product (single-host: one repo → one image). */
export interface ProductDto {
  id: string;
  slug: string;
  name: string;
  repo: string;
  status: ProductStatus;
  createdAtUtc: string;
}

/** Body for registering a product. */
export interface CreateProductRequest {
  slug: string;
  name: string;
  repo: string;
}

/** Body for updating a product (slug is immutable). */
export interface UpdateProductRequest {
  name: string;
  repo: string;
  status: ProductStatus;
}

/** Whether a product has a ready, deployable image (mirrors the backend enum, serialized as a string). */
export type ProductVersionState =
  | 'NoCi'
  | 'NeverBuilt'
  | 'UnreleasedBuild'
  | 'BuildPending'
  | 'BuildFailed'
  | 'Ready'
  | 'LatestNotReady'
  | 'Unknown';

/** A product's resolved build/image status — the latest released version and the newest with a ready image. */
export interface ProductVersionDto {
  state: ProductVersionState;
  latestTag: string | null;
  latestAtUtc: string | null;
  readyTag: string | null;
  readyAtUtc: string | null;
  image: string | null;
  /** Short human-readable reason for the state — shown on hover. */
  detail: string | null;
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

/**
 * Success envelope the backend wraps every resource 2xx body in (`ApiResponse<T>`):
 * the payload travels under `data`. Errors are NOT enveloped — they go out as
 * {@link ProblemDetails} (RFC 7807) and are read off the failed response directly.
 */
export interface ApiResponse<T> {
  data: T;
}

/** The signed-in admin, from GET /api/identity/me. */
export interface CurrentUser {
  login: string;
  name: string;
  avatar: string | null;
}

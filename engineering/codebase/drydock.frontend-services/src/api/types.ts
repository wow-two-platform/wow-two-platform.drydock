/** Providers implemented by the fleet catalog; adding one requires code. */
export enum VpsProvider {
  Hetzner = 'Hetzner',
}

/** A configured deployment host. */
export interface ServerDto {
  id: string;
  name: string;
  provider: VpsProvider;
  host: string;
  sshUser: string;
  region: string;
}

/** Lifecycle state of a portfolio product (mirrors the backend enum, serialized as a string). */
export type ProductStatus = 'Draft' | 'Active' | 'Paused' | 'Killed';

/** A registered portfolio product (one repo → a release bundle). */
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

/**
 * Success envelope the backend wraps every resource 2xx body in (`ApiResponse<T>`):
 * the payload travels under `data`. Errors are NOT enveloped — they go out as
 * RFC 7807 `ProblemDetails` (the SDK `foundation/http` type) and are read off the
 * failed response directly.
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

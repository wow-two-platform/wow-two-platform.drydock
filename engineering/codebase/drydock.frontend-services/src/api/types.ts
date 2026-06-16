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

/** The signed-in admin, from GET /api/auth/me. */
export interface CurrentUser {
  login: string;
  name: string;
  avatar: string | null;
}

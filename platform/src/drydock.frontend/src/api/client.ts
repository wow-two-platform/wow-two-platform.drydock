// Thin same-origin API client for the Drydock management plane.
// All URLs are relative ("/api/...") because the SPA is served from the .NET host
// (and proxied to the backend in dev — see vite.config.ts).
import type { ProblemDetails, RegisterServerRequest, ServerDto, SystemStatus } from './types';

/** Error carrying the server's RFC 7807 detail (or a transport-level message). */
export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(message: string, status: number, problem: ProblemDetails | null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

/** Extracts the best human-readable message from a failed response body. */
async function toApiError(res: Response): Promise<ApiError> {
  let problem: ProblemDetails | null = null;
  let detail: string | undefined;
  try {
    const text = await res.text();
    if (text) {
      const parsed: unknown = JSON.parse(text);
      if (parsed && typeof parsed === 'object') {
        problem = parsed as ProblemDetails;
        detail = problem.detail ?? problem.title;
      }
    }
  } catch {
    // Non-JSON error body — fall through to status text.
  }
  return new ApiError(detail ?? res.statusText ?? `Request failed (${res.status})`, res.status, problem);
}

/**
 * Init for {@link request}. `signal` is normalized to `null` for `fetch` so callers
 * can pass an optional signal directly under `exactOptionalPropertyTypes`.
 */
type RequestInitWithSignal = Omit<RequestInit, 'signal'> & {
  signal?: AbortSignal | null | undefined;
};

/** Performs a fetch and returns parsed JSON, or throws an {@link ApiError}. */
async function request<T>(input: string, init?: RequestInitWithSignal): Promise<T> {
  const { signal, ...rest } = init ?? {};
  let res: Response;
  try {
    res = await fetch(input, {
      ...rest,
      signal: signal ?? null,
      headers: {
        Accept: 'application/json',
        ...(rest.body ? { 'Content-Type': 'application/json' } : {}),
        ...rest.headers,
      },
    });
  } catch (cause) {
    throw new ApiError(cause instanceof Error ? cause.message : 'Network request failed', 0, null);
  }

  if (!res.ok) {
    throw await toApiError(res);
  }

  if (res.status === 204 || res.headers.get('Content-Length') === '0') {
    return undefined as T;
  }
  const text = await res.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

export const api = {
  /** GET /api/system/status — service liveness. */
  getStatus(signal?: AbortSignal): Promise<SystemStatus> {
    return request<SystemStatus>('/api/system/status', { signal });
  },

  /** GET /api/servers — all registered servers. */
  listServers(signal?: AbortSignal): Promise<ServerDto[]> {
    return request<ServerDto[]>('/api/servers', { signal });
  },

  /** POST /api/servers — register a new deploy-target server. */
  registerServer(body: RegisterServerRequest): Promise<ServerDto> {
    return request<ServerDto>('/api/servers', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },
};

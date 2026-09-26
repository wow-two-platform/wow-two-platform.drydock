// Same-origin HTTP transport for the Wheelhouse management API. The SPA is served by the .NET host,
// and the dev server proxies "/api" to it, so every URL stays relative.
import { ApiError, type ProblemDetails } from '@wow-two-beta/ui/foundation/http';

// The SDK transport error; the forms engine maps its ProblemDetails field errors onto form fields.
export { ApiError };

/** Success envelope around every resource body; failures arrive as un-enveloped ProblemDetails. */
interface ApiResponse<T> {
  data: T;
}

/** Init for {@link request}; `signal` accepts `undefined` under `exactOptionalPropertyTypes`. */
export type RequestOptions = Omit<RequestInit, 'signal'> & {
  signal?: AbortSignal | null | undefined;
  /** An explicit action header; the API requires it on state-changing operator actions. */
  action?: string | undefined;
};

/** Builds an {@link ApiError}, preferring the server's ProblemDetails detail over the status text. */
async function toApiError(res: Response): Promise<ApiError> {
  let problem: ProblemDetails | null = null;
  try {
    const text = await res.text();
    const parsed: unknown = text ? JSON.parse(text) : null;
    if (parsed && typeof parsed === 'object') problem = parsed as ProblemDetails;
  } catch {
    // A non-JSON body falls back to the status text.
  }
  return new ApiError(res.status, problem, problem?.detail ?? problem?.title ?? (res.statusText || `Request failed (${res.status})`));
}

/** Performs a request and returns the parsed JSON body, or throws an {@link ApiError}. */
export async function request<T>(input: string, options?: RequestOptions): Promise<T> {
  const { signal, action, ...rest } = options ?? {};
  let res: Response;
  try {
    res = await fetch(input, {
      ...rest,
      signal: signal ?? null,
      headers: {
        Accept: 'application/json',
        ...(rest.body ? { 'Content-Type': 'application/json' } : {}),
        ...(action ? { 'X-Wheelhouse-Action': action } : {}),
        ...rest.headers,
      },
    });
  } catch (cause) {
    throw new ApiError(0, null, cause instanceof Error ? cause.message : 'Network request failed');
  }
  if (!res.ok) throw await toApiError(res);
  if (res.status === 204 || res.headers.get('Content-Length') === '0') return undefined as T;
  const text = await res.text();
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

/** Performs a request whose success body is enveloped and returns its `data`. */
export async function requestData<T>(input: string, options?: RequestOptions): Promise<T> {
  return (await request<ApiResponse<T>>(input, options)).data;
}

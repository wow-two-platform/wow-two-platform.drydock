import {
  createApiClient,
  createRequestScope,
  ApiFailureFactory,
  type ApiFailure,
} from "@wow-two-beta/ui-vue/foundation/http";
import {
  ResultExtensions,
  type Result,
} from "@wow-two-beta/ui-vue/foundation/results";
import type { ZodType } from "zod";

const sessionScope = createRequestScope();
const client = createApiClient({
  credentials: "same-origin",
  scope: sessionScope,
});

/** Options for same-origin management requests; action headers guard operator writes. */
export interface RequestOptions {
  readonly method?: string;
  readonly body?: unknown;
  readonly signal?: AbortSignal | undefined;
  readonly action?: string;
}

/** Invalidates pending private reads and writes across a sign-out boundary. */
export function clearHttpSession(): void {
  sessionScope.invalidate();
}

/** Decodes the endpoint's consumed shape without exposing response diagnostics as display text. */
function decoder<T>(
  schema: ZodType,
): (value: unknown) => Result<T, ApiFailure> {
  return (value) => {
    const parsed = schema.safeParse(value);
    return parsed.success
      ? ResultExtensions.ok(parsed.data as T)
      : ResultExtensions.fail(ApiFailureFactory.create("protocol"));
  };
}

/** Reads an un-enveloped JSON endpoint through the SDK transport. */
export function request<T>(
  path: string,
  schema: ZodType,
  options: RequestOptions = {},
) {
  return client.request<T>(path, {
    ...options,
    signal: options.signal ?? null,
    unwrap: false,
    decode: decoder<T>(schema),
    ...(options.action
      ? { headers: { "X-Wheelhouse-Action": options.action } }
      : {}),
  });
}

/** Reads a management endpoint's required data envelope and validates its consumed payload. */
export function requestData<T>(
  path: string,
  schema: ZodType,
  options: RequestOptions = {},
) {
  return client.request<T>(path, {
    ...options,
    signal: options.signal ?? null,
    decode: decoder<T>(schema),
    ...(options.action
      ? { headers: { "X-Wheelhouse-Action": options.action } }
      : {}),
  });
}

/** Accepts an explicitly empty successful response for logout and resource deletion. */
export function requestEmpty(path: string, options: RequestOptions = {}) {
  return client.request(path, {
    ...options,
    signal: options.signal ?? null,
    response: "empty",
    ...(options.action
      ? { headers: { "X-Wheelhouse-Action": options.action } }
      : {}),
  });
}

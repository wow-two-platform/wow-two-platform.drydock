import type { CurrentUser } from '@/domain/auth';
import { request } from '@/integration/common';

/** The GitHub sign-in session endpoints. */
export const authApi = {
  /** Reads the signed-in operator; throws a 401 `ApiError` when there is no session. */
  getCurrentUser: (signal?: AbortSignal) => request<CurrentUser>('/api/identity/me', { signal }),

  /** Clears the session cookie. */
  signOut: () => request<void>('/api/identity/sign-out', { method: 'POST' }),

  /** The path the browser navigates to for GitHub OAuth — a full redirect, not a fetch. */
  signInUrl: (returnUrl: string = window.location.pathname) =>
    `/api/identity/sign-in?returnUrl=${encodeURIComponent(returnUrl)}`,
};

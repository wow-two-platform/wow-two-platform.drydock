import { useCallback, useEffect, useState } from 'react';
import { api, ApiError, loginUrl } from '../api/client';
import type { CurrentUser } from '../api/types';

interface AuthState {
  /** The signed-in admin, or null when not authenticated. */
  user: CurrentUser | null;
  /** True while the initial /api/auth/me check is in flight. */
  loading: boolean;
  /** Begins GitHub OAuth by navigating the browser to the login challenge. */
  signIn: () => void;
  /** Clears the session cookie and drops back to the sign-in screen. */
  signOut: () => Promise<void>;
}

// Module-level dedupe of the session check. StrictMode mounts twice in dev, so the effect runs
// twice; sharing one in-flight promise means a single /api/auth/me request (not a duplicate that
// gets aborted) — which also removes the sign-in flicker the aborted request used to cause.
let meRequest: Promise<CurrentUser | null> | undefined;
function checkSession(): Promise<CurrentUser | null> {
  meRequest ??= api.getCurrentUser().catch((e) => {
    // 401 = simply not signed in; any other failure also lands on the sign-in screen.
    if (!(e instanceof ApiError) || e.status !== 401) console.error('Auth check failed:', e);
    return null;
  });
  return meRequest;
}

/** Resolves the current session via GET /api/auth/me and exposes sign-in / sign-out. */
export function useAuth(): AuthState {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    checkSession().then((u) => {
      if (!active) return;
      setUser(u);
      setLoading(false);
    });
    return () => {
      active = false;
    };
  }, []);

  const signIn = useCallback(() => {
    window.location.href = loginUrl();
  }, []);

  const signOut = useCallback(async () => {
    try {
      await api.logout();
    } finally {
      meRequest = undefined; // drop the cached session so a later check re-fetches
      setUser(null);
    }
  }, []);

  return { user, loading, signIn, signOut };
}

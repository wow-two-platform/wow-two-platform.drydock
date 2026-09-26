import { useCallback, useEffect, useState } from 'react';
import type { CurrentUser } from '@/domain/auth';
import { authApi } from '@/integration/auth';
import { ApiError } from '@/integration/common';

/** The session a signed-in operator carries through the dashboard. */
export interface AuthSession {
  user: CurrentUser | null;
  loading: boolean;
  signIn: () => void;
  signOut: () => Promise<void>;
}

// StrictMode mounts twice in development; one shared request avoids a duplicate session check.
let sessionRequest: Promise<CurrentUser | null> | undefined;

function checkSession(): Promise<CurrentUser | null> {
  sessionRequest ??= authApi.getCurrentUser().catch((error: unknown) => {
    if (!(error instanceof ApiError) || error.status !== 401) console.error('Session check failed:', error);
    return null;
  });
  return sessionRequest;
}

/** Resolves the GitHub session and exposes sign-in and sign-out. */
export function useAuth(): AuthSession {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    void checkSession().then((resolved) => {
      if (!active) return;
      setUser(resolved);
      setLoading(false);
    });
    return () => {
      active = false;
    };
  }, []);

  const signIn = useCallback(() => {
    window.location.href = authApi.signInUrl();
  }, []);

  const signOut = useCallback(async () => {
    try {
      await authApi.signOut();
    } finally {
      sessionRequest = undefined;
      setUser(null);
    }
  }, []);

  return { user, loading, signIn, signOut };
}

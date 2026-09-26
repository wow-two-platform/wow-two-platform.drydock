import { useCallback, useEffect, useRef, useState } from 'react';

// Shim of `useRefresh` from @wow-two-beta/ui/query (added in the SDK, unpublished at 0.0.108).
// Swap the import to the SDK on the next re-pin and delete this file.

/** Tracks a user-requested refresh, holding `refreshing` for at least `minDuration` ms so the skeleton swap is seen. */
export function useRefresh(refetch: () => Promise<unknown>, { minDuration = 400 }: { minDuration?: number } = {}) {
  const [refreshing, setRefreshing] = useState(false);
  const pending = useRef(0);
  const mounted = useRef(true);

  // Set on every mount: StrictMode mounts, unmounts and mounts again, and a flag cleared by the first cleanup
  // would otherwise leave `refreshing` stuck on true.
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  const refresh = useCallback(async () => {
    pending.current += 1;
    setRefreshing(true);
    try {
      await Promise.all([refetch().catch(() => undefined), new Promise((resolve) => setTimeout(resolve, minDuration))]);
    } finally {
      pending.current -= 1;
      if (mounted.current && pending.current === 0) setRefreshing(false);
    }
  }, [refetch, minDuration]);

  return { refresh, refreshing } as const;
}

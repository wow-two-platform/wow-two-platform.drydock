import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { ServerDto } from '../api/types';

interface ServersState {
  servers: ServerDto[];
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
}

/** Loads and refreshes the registered-server roster. */
export function useServers(): ServersState {
  const [servers, setServers] = useState<ServerDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setServers(await api.listServers());
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to load servers.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { servers, loading, error, reload };
}

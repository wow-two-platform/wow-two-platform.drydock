import { useEffect, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { ProductVersionDto } from '../api/types';

interface ProductVersionState {
  version: ProductVersionDto | null;
  loading: boolean;
  error: string | null;
}

/** Lazily resolves a single product's build/image status — fetched when the row mounts. */
export function useProductVersion(productId: string): ProductVersionState {
  const [version, setVersion] = useState<ProductVersionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);

    api
      .getProductVersion(productId, controller.signal)
      .then((v) => setVersion(v))
      .catch((e: unknown) => {
        if (controller.signal.aborted) return;
        setError(e instanceof ApiError ? e.message : 'Failed to load version.');
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [productId]);

  return { version, loading, error };
}

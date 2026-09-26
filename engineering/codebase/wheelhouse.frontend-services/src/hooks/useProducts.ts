import { useCallback, useEffect, useState } from 'react';
import { api, ApiError } from '../api/client';
import type { CreateProductRequest, ProductDto, UpdateProductRequest } from '../api/types';

interface ProductsState {
  products: ProductDto[];
  loading: boolean;
  error: string | null;
  reload: () => Promise<void>;
  create: (body: CreateProductRequest) => Promise<ProductDto>;
  update: (id: string, body: UpdateProductRequest) => Promise<ProductDto>;
  remove: (id: string) => Promise<void>;
}

/** Loads and refreshes the registered-product roster, with create / update / delete mutators. */
export function useProducts(): ProductsState {
  const [products, setProducts] = useState<ProductDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setProducts(await api.listProducts());
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Failed to load products.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void reload();
  }, [reload]);

  const create = useCallback(
    async (body: CreateProductRequest) => {
      const created = await api.createProduct(body);
      await reload();
      return created;
    },
    [reload],
  );

  const update = useCallback(
    async (id: string, body: UpdateProductRequest) => {
      const updated = await api.updateProduct(id, body);
      await reload();
      return updated;
    },
    [reload],
  );

  const remove = useCallback(
    async (id: string) => {
      await api.deleteProduct(id);
      await reload();
    },
    [reload],
  );

  return { products, loading, error, reload, create, update, remove };
}

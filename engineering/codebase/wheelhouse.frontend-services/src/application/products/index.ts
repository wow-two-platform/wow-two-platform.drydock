import { useAppMutation, useAppQuery } from '@wow-two-beta/ui/query';
import type { CreateProductRequest, Product, UpdateProductRequest } from '@/domain/products';
import { productsApi } from '@/integration/products';

const ProductKeys = { list: ['products'] as const };

/** The product registry with its create, update and delete operations. */
export function useProducts() {
  const list = useAppQuery({ key: ProductKeys.list, queryFn: ({ signal }) => productsApi.listProducts(signal) });
  const invalidates = () => [ProductKeys.list];
  const create = useAppMutation({ mutationFn: (body: CreateProductRequest) => productsApi.createProduct(body), invalidates });
  const update = useAppMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateProductRequest }) => productsApi.updateProduct(id, body),
    invalidates,
  });
  const remove = useAppMutation({ mutationFn: (id: string) => productsApi.deleteProduct(id), invalidates });

  return {
    products: list.data ?? ([] as Product[]),
    loading: list.loading,
    error: list.error?.message ?? null,
    reload: async () => {
      await list.refetch();
    },
    create: (body: CreateProductRequest) => create.mutateAsync(body),
    update: (id: string, body: UpdateProductRequest) => update.mutateAsync({ id, body }),
    remove: (id: string) => remove.mutateAsync(id),
  };
}

/** The registry operations a product form submits through. */
export type ProductOperations = ReturnType<typeof useProducts>;

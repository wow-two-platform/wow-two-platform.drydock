import { computed } from "vue";
import { useAppMutation, useAppQuery } from "@/bootstrap/query";
import type {
  CreateProductRequest,
  UpdateProductRequest,
} from "@/domain/products";
import { productsApi } from "@/integration/products";

const ProductKeys = { list: ["products"] as const };

/** The portfolio registry with confirmed, result-returning mutations. */
export function useProducts() {
  const list = useAppQuery({
    key: ProductKeys.list,
    queryFn: ({ signal }) => productsApi.listProducts(signal),
  });
  const invalidates = () => [ProductKeys.list];
  const create = useAppMutation({
    mutationFn: (body: CreateProductRequest, { signal }) =>
      productsApi.createProduct(body, signal),
    invalidates,
  });
  const update = useAppMutation({
    mutationFn: (
      { id, body }: { id: string; body: UpdateProductRequest },
      { signal },
    ) => productsApi.updateProduct(id, body, signal),
    invalidates,
  });
  const remove = useAppMutation({
    mutationFn: (id: string, { signal }) =>
      productsApi.deleteProduct(id, signal),
    invalidates,
  });
  const products = computed(() => list.data.value ?? []);
  const error = computed(() => list.error.value?.message ?? null);

  return {
    products,
    loading: list.loading,
    error,
    reload: async () => {
      await list.refetch();
    },
    create: (body: CreateProductRequest) => create.mutateAsync(body),
    update: (id: string, body: UpdateProductRequest) =>
      update.mutateAsync({ id, body }),
    remove: (id: string) => remove.mutateAsync(id),
  };
}

/** Operations accepted by the registration and editing form. */
export type ProductOperations = ReturnType<typeof useProducts>;

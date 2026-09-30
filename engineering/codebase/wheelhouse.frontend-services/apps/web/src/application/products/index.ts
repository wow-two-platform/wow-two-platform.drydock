import { computed } from "vue";
import { useAppMutation, useAppQuery } from "@/bootstrap/query";
import type { ProductLifecycle } from "@/domain/products";
import { productsApi } from "@/integration/products";

const ProductKeys = { list: ["products"] as const };

/** The code-owned product catalog, with the operator's confirmed lifecycle changes. */
export function useProducts() {
  const list = useAppQuery({
    key: ProductKeys.list,
    queryFn: ({ signal }) => productsApi.listProducts(signal),
  });
  const lifecycle = useAppMutation({
    mutationFn: (
      { slug, value }: { slug: string; value: ProductLifecycle },
      { signal },
    ) => productsApi.updateLifecycle(slug, value, signal),
    invalidates: () => [ProductKeys.list],
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
    setLifecycle: (slug: string, value: ProductLifecycle) =>
      lifecycle.mutateAsync({ slug, value }),
  };
}

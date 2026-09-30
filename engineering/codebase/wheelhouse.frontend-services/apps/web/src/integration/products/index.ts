import { z } from "zod";
import { ProductLifecycle, type Product } from "@/domain/products";
import { requestData } from "@/integration/common";

const ProductSchema = z.object({
  slug: z.string(),
  name: z.string(),
  description: z.string(),
  lifecycle: z.enum(ProductLifecycle),
  repository: z.object({
    name: z.string(),
    url: z.string(),
    defaultBranch: z.string(),
  }),
  iconUrl: z.string(),
  environments: z.array(
    z.object({
      name: z.string(),
      sites: z.array(
        z.object({
          name: z.string(),
          url: z.string(),
          exposure: z.enum(["public", "private"]),
        }),
      ),
      secrets: z.object({ vault: z.string(), namespace: z.string() }).nullable(),
    }),
  ),
});

/** The code-owned product catalog, with validated responses and an explicit lifecycle write. */
export const productsApi = {
  listProducts: (signal?: AbortSignal) =>
    requestData<Product[]>("/api/products", ProductSchema.array(), { signal }),
  updateLifecycle: (
    slug: string,
    lifecycle: ProductLifecycle,
    signal?: AbortSignal,
  ) =>
    requestData<Product>(
      `/api/products/${encodeURIComponent(slug)}/lifecycle`,
      ProductSchema,
      { method: "PUT", body: { lifecycle }, signal, action: "lifecycle" },
    ),
};

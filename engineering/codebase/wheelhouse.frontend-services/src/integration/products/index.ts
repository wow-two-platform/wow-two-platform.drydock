import { z } from "zod";
import {
  ProductStatus,
  type CreateProductRequest,
  type Product,
  type UpdateProductRequest,
} from "@/domain/products";
import { requestData, requestEmpty } from "@/integration/common";

const ProductSchema = z.object({
  id: z.string(),
  slug: z.string(),
  name: z.string(),
  repo: z.string(),
  status: z.enum(ProductStatus),
  createdAtUtc: z.string(),
});

/** The portfolio registry, with validated responses and explicit mutation cancellation. */
export const productsApi = {
  listProducts: (signal?: AbortSignal) =>
    requestData<Product[]>("/api/products", ProductSchema.array(), { signal }),
  createProduct: (body: CreateProductRequest, signal?: AbortSignal) =>
    requestData<Product>("/api/products", ProductSchema, {
      method: "POST",
      body,
      signal,
    }),
  updateProduct: (
    id: string,
    body: UpdateProductRequest,
    signal?: AbortSignal,
  ) =>
    requestData<Product>(
      `/api/products/${encodeURIComponent(id)}`,
      ProductSchema,
      { method: "PUT", body, signal },
    ),
  deleteProduct: (id: string, signal?: AbortSignal) =>
    requestEmpty(`/api/products/${encodeURIComponent(id)}`, {
      method: "DELETE",
      signal,
    }),
};

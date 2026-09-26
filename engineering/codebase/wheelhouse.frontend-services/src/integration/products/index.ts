import type { CreateProductRequest, Product, UpdateProductRequest } from '@/domain/products';
import { request, requestData } from '@/integration/common';

/** The portfolio product registry. */
export const productsApi = {
  listProducts: (signal?: AbortSignal) => requestData<Product[]>('/api/products', { signal }),

  createProduct: (body: CreateProductRequest) =>
    requestData<Product>('/api/products', { method: 'POST', body: JSON.stringify(body) }),

  updateProduct: (id: string, body: UpdateProductRequest) =>
    requestData<Product>(`/api/products/${id}`, { method: 'PUT', body: JSON.stringify(body) }),

  deleteProduct: (id: string) => request<void>(`/api/products/${id}`, { method: 'DELETE' }),
};

/** A portfolio product's lifecycle state. */
export const ProductStatus = {
  Draft: 'Draft',
  Active: 'Active',
  Paused: 'Paused',
  Killed: 'Killed',
} as const;
export type ProductStatus = (typeof ProductStatus)[keyof typeof ProductStatus];

/** A registered portfolio product and its source repository. */
export interface Product {
  id: string;
  slug: string;
  name: string;
  repo: string;
  status: ProductStatus;
  createdAtUtc: string;
}

/** Registers a product. */
export interface CreateProductRequest {
  slug: string;
  name: string;
  repo: string;
}

/** Updates a product; its slug is immutable. */
export interface UpdateProductRequest {
  name: string;
  repo: string;
  status: ProductStatus;
}

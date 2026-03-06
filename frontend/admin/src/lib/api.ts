const gatewayUrl = (): string =>
  (typeof window !== 'undefined'
    ? (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined)
    : undefined) ?? 'http://localhost:8080';

export type Brand = {
  id: string;
  name: string;
  slug: string;
  description: string;
};

export type Category = {
  id: string;
  name: string;
  slug: string;
  description: string;
};

export type Collection = {
  id: string;
  name: string;
  slug: string;
  description: string;
  isFeatured: boolean;
};

export type Product = {
  id: string;
  sku: string;
  name: string;
  description: string;
  price: number;
  brandId: string;
  categoryId: string;
  collectionIds: string[];
  isNewArrival: boolean;
  isBestSeller: boolean;
};

export type ProductInput = {
  sku: string;
  name: string;
  description: string;
  price: number;
  brandId: string;
  categoryId: string;
  collectionIds: string[];
  isNewArrival: boolean;
  isBestSeller: boolean;
};

export type OrderView = {
  id: string;
  cartId: string;
  userId: string;
  status: string;
  totalAmount: number;
  items: Array<{ productId: string; sku: string; name: string; quantity: number; unitPrice: number }>;
  trackingCode: string | null;
  transactionId: string | null;
  failureReason: string | null;
};

export async function fetchBrands(): Promise<Brand[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/brands`);
}

export async function createBrand(payload: Omit<Brand, 'id'>): Promise<Brand> {
  return postJson(`${gatewayUrl()}/api/catalog/v1/brands`, payload);
}

export async function updateBrand(id: string, payload: Omit<Brand, 'id'>): Promise<Brand> {
  return putJson(`${gatewayUrl()}/api/catalog/v1/brands/${id}`, payload);
}

export async function deleteBrand(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/catalog/v1/brands/${id}`);
}

export async function fetchCategories(): Promise<Category[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/categories`);
}

export async function createCategory(payload: Omit<Category, 'id'>): Promise<Category> {
  return postJson(`${gatewayUrl()}/api/catalog/v1/categories`, payload);
}

export async function updateCategory(id: string, payload: Omit<Category, 'id'>): Promise<Category> {
  return putJson(`${gatewayUrl()}/api/catalog/v1/categories/${id}`, payload);
}

export async function deleteCategory(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/catalog/v1/categories/${id}`);
}

export async function fetchCollections(): Promise<Collection[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/collections`);
}

export async function createCollection(payload: Omit<Collection, 'id'>): Promise<Collection> {
  return postJson(`${gatewayUrl()}/api/catalog/v1/collections`, payload);
}

export async function updateCollection(id: string, payload: Omit<Collection, 'id'>): Promise<Collection> {
  return putJson(`${gatewayUrl()}/api/catalog/v1/collections/${id}`, payload);
}

export async function deleteCollection(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/catalog/v1/collections/${id}`);
}

export async function fetchProducts(): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/products`);
}

export async function createProduct(payload: ProductInput): Promise<Product> {
  return postJson(`${gatewayUrl()}/api/catalog/v1/products`, payload);
}

export async function updateProduct(id: string, payload: ProductInput): Promise<Product> {
  return putJson(`${gatewayUrl()}/api/catalog/v1/products/${id}`, payload);
}

export async function deleteProduct(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/catalog/v1/products/${id}`);
}

export async function fetchOrder(orderId: string): Promise<OrderView> {
  const res = await fetch(`${gatewayUrl()}/api/order/v1/orders/${orderId}`);
  if (res.status === 404) {
    throw new Error('Order not found');
  }
  if (!res.ok) {
    throw new Error(`Order fetch error: ${res.status}`);
  }
  return res.json();
}

export async function fetchOrders(limit = 50): Promise<OrderView[]> {
  return fetchJson(`${gatewayUrl()}/api/order/v1/orders?limit=${limit}`);
}

export async function upsertStock(payload: { productId: string; sku: string; availableQuantity: number }): Promise<void> {
  await postJson(`${gatewayUrl()}/api/warehouse/v1/stock`, payload);
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  if (!response.ok) throw new Error(`API error: ${response.status}`);
  return response.json() as Promise<T>;
}

async function postJson<T>(url: string, payload: unknown): Promise<T> {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    const err = await response.json().catch(() => null);
    throw new Error(err?.detail ?? `POST error: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function putJson<T>(url: string, payload: unknown): Promise<T> {
  const response = await fetch(url, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    const err = await response.json().catch(() => null);
    throw new Error(err?.detail ?? `PUT error: ${response.status}`);
  }

  return response.json() as Promise<T>;
}

async function deleteJson(url: string): Promise<void> {
  const response = await fetch(url, { method: 'DELETE' });
  if (!response.ok && response.status !== 404) {
    const err = await response.json().catch(() => null);
    throw new Error(err?.detail ?? `DELETE error: ${response.status}`);
  }
}

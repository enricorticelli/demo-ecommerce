const gatewayUrl = (): string =>
  (typeof window !== 'undefined'
    ? (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined)
    : undefined) ?? 'http://localhost:8080';

const defaultRequestTimeoutMs = Number(
  (typeof window !== 'undefined'
    ? (import.meta.env.PUBLIC_API_TIMEOUT_MS as string | undefined)
    : undefined) ?? '20000'
);

const requestTimeoutMs = Number.isFinite(defaultRequestTimeoutMs) && defaultRequestTimeoutMs > 0
  ? defaultRequestTimeoutMs
  : 20000;

function isSafeMethod(method?: string): boolean {
  if (!method) return true;
  const normalized = method.toUpperCase();
  return normalized === 'GET' || normalized === 'HEAD';
}

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
  identityType: 'Anonymous' | 'Registered';
  paymentMethod: 'stripe_card' | 'paypal' | 'satispay';
  authenticatedUserId: string | null;
  anonymousId: string | null;
  customer: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
  };
  shippingAddress: {
    street: string;
    city: string;
    postalCode: string;
    country: string;
  };
  billingAddress: {
    street: string;
    city: string;
    postalCode: string;
    country: string;
  };
  status: string;
  totalAmount: number;
  items: Array<{ productId: string; sku: string; name: string; quantity: number; unitPrice: number }>;
  trackingCode: string | null;
  transactionId: string | null;
  failureReason: string | null;
};

export type PaginationParams = {
  limit?: number;
  offset?: number;
};

function buildPaginationQuery(params?: PaginationParams): string {
  const limit = params?.limit ?? 20;
  const offset = params?.offset ?? 0;
  return `limit=${Math.max(1, limit)}&offset=${Math.max(0, offset)}`;
}

export async function fetchBrands(params?: PaginationParams): Promise<Brand[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/brands?${buildPaginationQuery(params)}`);
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

export async function fetchCategories(params?: PaginationParams): Promise<Category[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/categories?${buildPaginationQuery(params)}`);
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

export async function fetchCollections(params?: PaginationParams): Promise<Collection[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/collections?${buildPaginationQuery(params)}`);
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

export async function fetchProducts(params?: PaginationParams): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/products?${buildPaginationQuery(params)}`);
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
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/order/v1/orders/${orderId}?includeNonCompleted=true`);
  if (res.status === 404) {
    throw new Error('Order not found');
  }
  if (!res.ok) {
    throw new Error(`Order fetch error: ${res.status}`);
  }
  return res.json();
}

export async function fetchOrders(limit = 50, offset = 0): Promise<OrderView[]> {
  return fetchJson(
    `${gatewayUrl()}/api/order/v1/orders?limit=${Math.max(1, limit)}&offset=${Math.max(0, offset)}&includeNonCompleted=true`
  );
}

export async function manualCompleteOrder(
  orderId: string,
  payload?: { trackingCode?: string; transactionId?: string }
): Promise<void> {
  await postJson(`${gatewayUrl()}/api/order/v1/orders/${orderId}/manual-complete`, {
    trackingCode: payload?.trackingCode ?? null,
    transactionId: payload?.transactionId ?? null
  });
}

export async function manualCancelOrder(orderId: string, reason?: string): Promise<void> {
  await postJson(`${gatewayUrl()}/api/order/v1/orders/${orderId}/manual-cancel`, {
    reason: reason?.trim() ? reason.trim() : null
  });
}

export async function upsertStock(payload: { productId: string; sku: string; availableQuantity: number }): Promise<void> {
  await postJson(`${gatewayUrl()}/api/warehouse/v1/stock`, payload);
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetchWithTimeout(url);
  if (!response.ok) throw new Error(`API error: ${response.status}`);
  return response.json() as Promise<T>;
}

async function postJson<T>(url: string, payload: unknown): Promise<T> {
  const response = await fetchWithTimeout(url, {
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
  const response = await fetchWithTimeout(url, {
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
  const response = await fetchWithTimeout(url, { method: 'DELETE' });
  if (!response.ok && response.status !== 404) {
    const err = await response.json().catch(() => null);
    throw new Error(err?.detail ?? `DELETE error: ${response.status}`);
  }
}

async function fetchWithTimeout(url: string, init?: RequestInit): Promise<Response> {
  const shouldRetry = isSafeMethod(init?.method);

  for (let attempt = 0; attempt < (shouldRetry ? 2 : 1); attempt += 1) {
    const controller = new AbortController();
    const timerId = globalThis.setTimeout(() => controller.abort(), requestTimeoutMs);

    try {
      return await fetch(url, { ...init, signal: controller.signal });
    } catch (error) {
      const isTimeout = error instanceof DOMException && error.name === 'AbortError';

      if (isTimeout && shouldRetry && attempt === 0) {
        continue;
      }

      if (isTimeout) {
        throw new Error(`Request timeout after ${requestTimeoutMs}ms`);
      }

      throw error;
    } finally {
      globalThis.clearTimeout(timerId);
    }
  }

  throw new Error(`Request timeout after ${requestTimeoutMs}ms`);
}

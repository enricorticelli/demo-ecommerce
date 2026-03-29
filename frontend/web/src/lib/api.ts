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

// ─── Types ────────────────────────────────────────────────────────────────────

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
  brandName: string;
  categoryId: string;
  categoryName: string;
  collectionIds: string[];
  collectionNames: string[];
  isNewArrival: boolean;
  isBestSeller: boolean;
  createdAtUtc: string;
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

export type CartItemDto = {
  productId: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
};

export type CartView = {
  cartId: string;
  userId: string;
  items: CartItemDto[];
  totalAmount: number;
};

export type OrderItemDto = {
  productId: string;
  sku: string;
  name: string;
  quantity: number;
  unitPrice: number;
};

export type OrderCustomerDetails = {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
};

export type OrderAddress = {
  street: string;
  city: string;
  postalCode: string;
  country: string;
};

export type OrderView = {
  id: string;
  cartId: string;
  userId: string;
  identityType: 'Anonymous' | 'Registered';
  paymentMethod: 'stripe_card' | 'paypal' | 'satispay';
  authenticatedUserId: string | null;
  anonymousId: string | null;
  customer: OrderCustomerDetails;
  shippingAddress: OrderAddress;
  billingAddress: OrderAddress;
  status: string;
  totalAmount: number;
  items: OrderItemDto[];
  trackingCode: string | null;
  transactionId: string | null;
  failureReason: string | null;
};

export type CreateOrderResult = {
  orderId: string;
  status: string;
};

export type CreateOrderPayload = {
  cartId: string;
  userId: string;
  identityType: 'Anonymous' | 'Registered';
  paymentMethod: 'stripe_card' | 'paypal' | 'satispay';
  items: OrderItemDto[];
  totalAmount: number;
  authenticatedUserId: string | null;
  anonymousId: string | null;
  customer: OrderCustomerDetails;
  shippingAddress: OrderAddress;
  billingAddress: OrderAddress;
};

type GuestRequestOptions = {
  anonymousId?: string;
  headers?: HeadersInit;
};

export type PaymentSession = {
  sessionId: string;
  orderId: string;
  userId: string;
  amount: number;
  paymentMethod: 'stripe_card' | 'paypal' | 'satispay';
  providerCode: string;
  externalCheckoutId: string | null;
  providerStatus: string;
  status: string;
  transactionId: string | null;
  failureReason: string | null;
  createdAtUtc: string;
  completedAtUtc: string | null;
  redirectUrl: string;
};

export type ShipmentView = {
  id: string;
  orderId: string;
  userId: string;
  trackingCode: string;
  status: 'Preparing' | 'Created' | 'InTransit' | 'Delivered' | 'Cancelled' | string;
  createdAtUtc: string;
  updatedAtUtc: string;
  deliveredAtUtc: string | null;
};

export type PaginationParams = {
  limit?: number;
  offset?: number;
};

function buildPaginationQuery(params?: PaginationParams): string {
  const limit = params?.limit ?? 200;
  const offset = params?.offset ?? 0;
  return `limit=${Math.max(1, limit)}&offset=${Math.max(0, offset)}`;
}

// ─── Catalog ─────────────────────────────────────────────────────────────────

export async function fetchProducts(params?: PaginationParams): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/store/catalog/v1/products?${buildPaginationQuery(params)}`);
}

export async function fetchNewArrivals(): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/store/catalog/v1/products/new-arrivals`);
}

export async function fetchBestSellers(): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/store/catalog/v1/products/best-sellers`);
}

export async function fetchProduct(id: string): Promise<Product> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/catalog/v1/products/${id}`);
  if (res.status === 404) throw new NotFoundError(`Product ${id} not found`);
  if (!res.ok) throw new Error(`Catalog error: ${res.status}`);
  return res.json();
}

export async function createProduct(_payload: ProductInput): Promise<Product> {
  return postJson<Product>(`${gatewayUrl()}/api/backoffice/catalog/v1/products`, _payload);
}

export async function updateProduct(_id: string, _payload: ProductInput): Promise<Product> {
  return putJson<Product>(`${gatewayUrl()}/api/backoffice/catalog/v1/products/${_id}`, _payload);
}

export async function deleteProduct(_id: string): Promise<void> {
  return deleteJson(`${gatewayUrl()}/api/backoffice/catalog/v1/products/${_id}`);
}

export async function fetchBrands(): Promise<Brand[]> {
  return fetchJson(`${gatewayUrl()}/api/backoffice/catalog/v1/brands?${buildPaginationQuery()}`);
}

export async function createBrand(_payload: Omit<Brand, 'id'>): Promise<Brand> {
  return postJson<Brand>(`${gatewayUrl()}/api/backoffice/catalog/v1/brands`, _payload);
}

export async function updateBrand(_id: string, _payload: Omit<Brand, 'id'>): Promise<Brand> {
  return putJson<Brand>(`${gatewayUrl()}/api/backoffice/catalog/v1/brands/${_id}`, _payload);
}

export async function deleteBrand(_id: string): Promise<void> {
  return deleteJson(`${gatewayUrl()}/api/backoffice/catalog/v1/brands/${_id}`);
}

export async function fetchCategories(): Promise<Category[]> {
  return fetchJson(`${gatewayUrl()}/api/backoffice/catalog/v1/categories?${buildPaginationQuery()}`);
}

export async function createCategory(_payload: Omit<Category, 'id'>): Promise<Category> {
  return postJson<Category>(`${gatewayUrl()}/api/backoffice/catalog/v1/categories`, _payload);
}

export async function updateCategory(_id: string, _payload: Omit<Category, 'id'>): Promise<Category> {
  return putJson<Category>(`${gatewayUrl()}/api/backoffice/catalog/v1/categories/${_id}`, _payload);
}

export async function deleteCategory(_id: string): Promise<void> {
  return deleteJson(`${gatewayUrl()}/api/backoffice/catalog/v1/categories/${_id}`);
}

export async function fetchCollections(): Promise<Collection[]> {
  return fetchJson(`${gatewayUrl()}/api/backoffice/catalog/v1/collections?${buildPaginationQuery()}`);
}

export async function createCollection(_payload: Omit<Collection, 'id'>): Promise<Collection> {
  return postJson<Collection>(`${gatewayUrl()}/api/backoffice/catalog/v1/collections`, _payload);
}

export async function updateCollection(_id: string, _payload: Omit<Collection, 'id'>): Promise<Collection> {
  return putJson<Collection>(`${gatewayUrl()}/api/backoffice/catalog/v1/collections/${_id}`, _payload);
}

export async function deleteCollection(_id: string): Promise<void> {
  return deleteJson(`${gatewayUrl()}/api/backoffice/catalog/v1/collections/${_id}`);
}

// ─── Cart ─────────────────────────────────────────────────────────────────────

export async function fetchCart(cartId: string, anonymousId: string): Promise<CartView | null> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/cart/v1/carts/${cartId}`, {
    headers: withGuestIdentityHeader(anonymousId),
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Cart error: ${res.status}`);
  return res.json();
}

export async function addCartItem(
  cartId: string,
  anonymousId: string,
  payload: {
    productId: string;
    sku: string;
    name: string;
    quantity: number;
    unitPrice: number;
  }
): Promise<void> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/cart/v1/carts/${cartId}/items`, {
    method: 'POST',
    headers: withGuestIdentityHeader(anonymousId, { 'Content-Type': 'application/json' }),
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.detail ?? `Cart add error: ${res.status}`);
  }
}

export async function removeCartItem(cartId: string, productId: string, anonymousId: string): Promise<void> {
  const res = await fetchWithTimeout(
    `${gatewayUrl()}/api/store/cart/v1/carts/${cartId}/items/${productId}`,
    { method: 'DELETE', headers: withGuestIdentityHeader(anonymousId) }
  );
  if (!res.ok) throw new Error(`Cart remove error: ${res.status}`);
}

// ─── Order ────────────────────────────────────────────────────────────────────

export async function createOrder(payload: CreateOrderPayload, options?: GuestRequestOptions): Promise<CreateOrderResult> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/order/v1/orders`, {
    method: 'POST',
    headers: withGuestIdentityHeader(options?.anonymousId, {
      'Content-Type': 'application/json',
      ...normalizeHeaders(options?.headers),
    }),
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.detail ?? `Order create error: ${res.status}`);
  }
  return res.json();
}

export async function fetchOrder(orderId: string, options?: GuestRequestOptions): Promise<OrderView> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/order/v1/orders/${orderId}`, {
    headers: withGuestIdentityHeader(options?.anonymousId, options?.headers),
    cache: 'no-store',
  });
  if (res.status === 404) throw new NotFoundError(`Order ${orderId} not found`);
  if (!res.ok) throw new Error(`Order fetch error: ${res.status}`);
  return res.json();
}

export async function manualCancelOrder(orderId: string, reason?: string, options?: GuestRequestOptions): Promise<void> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/order/v1/orders/${orderId}/manual-cancel`, {
    method: 'POST',
    headers: withGuestIdentityHeader(options?.anonymousId, {
      'Content-Type': 'application/json',
      ...normalizeHeaders(options?.headers),
    }),
    body: JSON.stringify({
      reason: reason?.trim() ? reason.trim() : null,
    }),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.detail ?? `Order cancel error: ${res.status}`);
  }
}

export async function getPaymentSessionByOrder(orderId: string, options?: GuestRequestOptions): Promise<PaymentSession | null> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/payment/v1/payments/sessions/orders/${orderId}`, {
    headers: withGuestIdentityHeader(options?.anonymousId, options?.headers),
    cache: 'no-store',
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Payment session error: ${res.status}`);
  return res.json();
}

export async function getPaymentSessionById(sessionId: string, options?: GuestRequestOptions): Promise<PaymentSession | null> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/payment/v1/payments/sessions/${sessionId}`, {
    headers: withGuestIdentityHeader(options?.anonymousId, options?.headers),
    cache: 'no-store',
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Payment session error: ${res.status}`);
  return res.json();
}

export async function fetchShipmentByOrder(orderId: string, options?: GuestRequestOptions): Promise<ShipmentView | null> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/store/shipping/v1/shipments/orders/${orderId}`, {
    headers: withGuestIdentityHeader(options?.anonymousId, options?.headers),
    cache: 'no-store',
  });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Shipment fetch error: ${res.status}`);
  return res.json();
}

// ─── Utilities ───────────────────────────────────────────────────────────────

export class NotFoundError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'NotFoundError';
  }
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
    body: JSON.stringify(payload),
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
    body: JSON.stringify(payload),
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

function normalizeHeaders(headers?: HeadersInit): Record<string, string> {
  if (!headers) return {};

  if (headers instanceof Headers) {
    return Object.fromEntries(headers.entries());
  }

  if (Array.isArray(headers)) {
    return Object.fromEntries(headers);
  }

  return { ...headers };
}

function withGuestIdentityHeader(anonymousId?: string, headers?: HeadersInit): Record<string, string> {
  const normalizedHeaders = normalizeHeaders(headers);
  const value = anonymousId?.trim();

  if (!value) {
    return normalizedHeaders;
  }

  return {
    ...normalizedHeaders,
    'X-Anonymous-Id': value,
  };
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

/** Poll order until it reaches a terminal state. Calls onUpdate on each poll tick. */
export async function pollOrderUntilDone(
  orderId: string,
  onUpdate: (order: OrderView) => void,
  maxAttempts = 40,
  intervalMs = 1000,
  options?: GuestRequestOptions
): Promise<OrderView | null> {
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const order = await fetchOrder(orderId, options);
      onUpdate(order);
      if (order.status === 'Completed' || order.status === 'Failed') return order;
    } catch {
      // transient error — keep polling
    }
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return null;
}

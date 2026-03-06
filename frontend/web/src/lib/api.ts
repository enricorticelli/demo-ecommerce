const gatewayUrl = (): string =>
  (typeof window !== 'undefined'
    ? (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined)
    : undefined) ?? 'http://localhost:8080';

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

export type OrderView = {
  id: string;
  cartId: string;
  userId: string;
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

export type PaymentSession = {
  sessionId: string;
  orderId: string;
  userId: string;
  amount: number;
  status: string;
  transactionId: string | null;
  failureReason: string | null;
  createdAtUtc: string;
  completedAtUtc: string | null;
  redirectUrl: string;
};

// ─── Catalog ─────────────────────────────────────────────────────────────────

export async function fetchProducts(): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/products`);
}

export async function fetchNewArrivals(): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/products/new-arrivals`);
}

export async function fetchBestSellers(): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/catalog/v1/products/best-sellers`);
}

export async function fetchProduct(id: string): Promise<Product> {
  const res = await fetch(`${gatewayUrl()}/api/catalog/v1/products/${id}`);
  if (res.status === 404) throw new NotFoundError(`Product ${id} not found`);
  if (!res.ok) throw new Error(`Catalog error: ${res.status}`);
  return res.json();
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

// ─── Cart ─────────────────────────────────────────────────────────────────────

export async function fetchCart(cartId: string): Promise<CartView | null> {
  const res = await fetch(`${gatewayUrl()}/api/cart/v1/carts/${cartId}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Cart error: ${res.status}`);
  return res.json();
}

export async function addCartItem(
  cartId: string,
  payload: {
    userId: string;
    productId: string;
    sku: string;
    name: string;
    quantity: number;
    unitPrice: number;
  }
): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/api/cart/v1/carts/${cartId}/items`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.detail ?? `Cart add error: ${res.status}`);
  }
}

export async function removeCartItem(cartId: string, productId: string): Promise<void> {
  const res = await fetch(
    `${gatewayUrl()}/api/cart/v1/carts/${cartId}/items/${productId}`,
    { method: 'DELETE' }
  );
  if (!res.ok) throw new Error(`Cart remove error: ${res.status}`);
}

// ─── Order ────────────────────────────────────────────────────────────────────

export async function createOrder(cartId: string, userId: string): Promise<CreateOrderResult> {
  const res = await fetch(`${gatewayUrl()}/api/order/v1/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ cartId, userId }),
  });
  if (!res.ok) {
    const err = await res.json().catch(() => null);
    throw new Error(err?.detail ?? `Order create error: ${res.status}`);
  }
  return res.json();
}

export async function fetchOrder(orderId: string): Promise<OrderView> {
  const res = await fetch(`${gatewayUrl()}/api/order/v1/orders/${orderId}`);
  if (res.status === 404) throw new NotFoundError(`Order ${orderId} not found`);
  if (!res.ok) throw new Error(`Order fetch error: ${res.status}`);
  return res.json();
}

export async function getPaymentSessionByOrder(orderId: string): Promise<PaymentSession | null> {
  const res = await fetch(`${gatewayUrl()}/api/payment/v1/payments/sessions/orders/${orderId}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Payment session error: ${res.status}`);
  return res.json();
}

export async function getPaymentSessionById(sessionId: string): Promise<PaymentSession | null> {
  const res = await fetch(`${gatewayUrl()}/api/payment/v1/payments/sessions/${sessionId}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Payment session error: ${res.status}`);
  return res.json();
}

export async function authorizePaymentSession(sessionId: string): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/api/payment/v1/payments/sessions/${sessionId}/authorize`, {
    method: 'POST',
  });

  if (!res.ok) throw new Error(`Payment authorize error: ${res.status}`);
}

export async function rejectPaymentSession(sessionId: string, reason = 'Payment declined'): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/api/payment/v1/payments/sessions/${sessionId}/reject`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ reason }),
  });

  if (!res.ok) throw new Error(`Payment reject error: ${res.status}`);
}

// ─── Utilities ───────────────────────────────────────────────────────────────

export class NotFoundError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'NotFoundError';
  }
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
    body: JSON.stringify(payload),
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
    body: JSON.stringify(payload),
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

/** Poll order until it reaches a terminal state. Calls onUpdate on each poll tick. */
export async function pollOrderUntilDone(
  orderId: string,
  onUpdate: (order: OrderView) => void,
  maxAttempts = 40,
  intervalMs = 1000
): Promise<OrderView | null> {
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const order = await fetchOrder(orderId);
      onUpdate(order);
      if (order.status === 'Completed' || order.status === 'Failed') return order;
    } catch {
      // transient error — keep polling
    }
    await new Promise((r) => setTimeout(r, intervalMs));
  }
  return null;
}

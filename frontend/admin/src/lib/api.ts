const gatewayUrl = (): string =>
  (typeof window === 'undefined'
    ? (import.meta.env.GATEWAY_INTERNAL_URL as string | undefined)
    : (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined))
  ?? (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined)
  ?? 'http://localhost:18080';

const ACCESS_COOKIE_NAME = 'bo_access_token';
const REFRESH_ENDPOINT_PATH = '/api/auth/refresh';

const defaultRequestTimeoutMs = Number(
  (typeof window !== 'undefined'
    ? (import.meta.env.PUBLIC_API_TIMEOUT_MS as string | undefined)
    : undefined) ?? '20000'
);

const requestTimeoutMs = Number.isFinite(defaultRequestTimeoutMs) && defaultRequestTimeoutMs > 0
  ? defaultRequestTimeoutMs
  : 20000;

let refreshInFlight: Promise<boolean> | null = null;
let loginRedirectTriggered = false;

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

export type WarehouseStockItem = {
  productId: string;
  sku: string;
  availableQuantity: number;
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

export type AdminCustomer = {
  id: string;
  username: string;
  email: string;
  isEmailVerified: boolean;
  firstName: string;
  lastName: string;
  phone: string;
  createdAtUtc: string;
};

export type AdminAccountUser = {
  id: string;
  username: string;
  email: string;
  createdAtUtc: string;
  permissions: string[];
  hasCustomPermissions: boolean;
};

export type AdminCustomerAddress = {
  id: string;
  label: string;
  street: string;
  city: string;
  postalCode: string;
  country: string;
  isDefaultShipping: boolean;
  isDefaultBilling: boolean;
};

export type PaginationParams = {
  limit?: number;
  offset?: number;
  searchTerm?: string;
};

function buildPaginationQuery(params?: PaginationParams): string {
  const limit = params?.limit ?? 20;
  const offset = params?.offset ?? 0;
  const query = new URLSearchParams({
    limit: String(Math.max(1, limit)),
    offset: String(Math.max(0, offset))
  });

  const normalizedSearch = params?.searchTerm?.trim();
  if (normalizedSearch)
  {
    query.set('searchTerm', normalizedSearch);
  }

  return query.toString();
}

export async function fetchBrands(params?: PaginationParams): Promise<Brand[]> {
  return fetchJson(`${gatewayUrl()}/api/admin/catalog/v1/brands?${buildPaginationQuery(params)}`);
}

export async function createBrand(payload: Omit<Brand, 'id'>): Promise<Brand> {
  return postJson(`${gatewayUrl()}/api/admin/catalog/v1/brands`, payload);
}

export async function updateBrand(id: string, payload: Omit<Brand, 'id'>): Promise<Brand> {
  return putJson(`${gatewayUrl()}/api/admin/catalog/v1/brands/${id}`, payload);
}

export async function deleteBrand(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/admin/catalog/v1/brands/${id}`);
}

export async function fetchCategories(params?: PaginationParams): Promise<Category[]> {
  return fetchJson(`${gatewayUrl()}/api/admin/catalog/v1/categories?${buildPaginationQuery(params)}`);
}

export async function createCategory(payload: Omit<Category, 'id'>): Promise<Category> {
  return postJson(`${gatewayUrl()}/api/admin/catalog/v1/categories`, payload);
}

export async function updateCategory(id: string, payload: Omit<Category, 'id'>): Promise<Category> {
  return putJson(`${gatewayUrl()}/api/admin/catalog/v1/categories/${id}`, payload);
}

export async function deleteCategory(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/admin/catalog/v1/categories/${id}`);
}

export async function fetchCollections(params?: PaginationParams): Promise<Collection[]> {
  return fetchJson(`${gatewayUrl()}/api/admin/catalog/v1/collections?${buildPaginationQuery(params)}`);
}

export async function createCollection(payload: Omit<Collection, 'id'>): Promise<Collection> {
  return postJson(`${gatewayUrl()}/api/admin/catalog/v1/collections`, payload);
}

export async function updateCollection(id: string, payload: Omit<Collection, 'id'>): Promise<Collection> {
  return putJson(`${gatewayUrl()}/api/admin/catalog/v1/collections/${id}`, payload);
}

export async function deleteCollection(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/admin/catalog/v1/collections/${id}`);
}

export async function fetchProducts(params?: PaginationParams): Promise<Product[]> {
  return fetchJson(`${gatewayUrl()}/api/admin/catalog/v1/products?${buildPaginationQuery(params)}`);
}

export async function createProduct(payload: ProductInput): Promise<Product> {
  return postJson(`${gatewayUrl()}/api/admin/catalog/v1/products`, payload);
}

export async function updateProduct(id: string, payload: ProductInput): Promise<Product> {
  return putJson(`${gatewayUrl()}/api/admin/catalog/v1/products/${id}`, payload);
}

export async function deleteProduct(id: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/admin/catalog/v1/products/${id}`);
}

export async function fetchOrder(orderId: string): Promise<OrderView> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/admin/order/v1/orders/${orderId}`);
  if (res.status === 404) {
    throw new Error('Order not found');
  }
  if (!res.ok) {
    throw new Error(await getApiErrorMessage(res, 'GET'));
  }
  return res.json();
}

export async function fetchOrders(limit = 50, offset = 0, searchTerm = ''): Promise<OrderView[]> {
  const query = new URLSearchParams({
    limit: String(Math.max(1, limit)),
    offset: String(Math.max(0, offset))
  });

  const normalizedSearch = searchTerm.trim();
  if (normalizedSearch)
  {
    query.set('searchTerm', normalizedSearch);
  }

  return fetchJson(
    `${gatewayUrl()}/api/admin/order/v1/orders?${query.toString()}`
  );
}

export async function manualCompleteOrder(
  orderId: string,
  payload?: { trackingCode?: string; transactionId?: string }
): Promise<void> {
  await postJson(`${gatewayUrl()}/api/admin/order/v1/orders/${orderId}/manual-complete`, {
    trackingCode: payload?.trackingCode ?? null,
    transactionId: payload?.transactionId ?? null
  });
}

export async function manualCancelOrder(orderId: string, reason?: string): Promise<void> {
  await postJson(`${gatewayUrl()}/api/admin/order/v1/orders/${orderId}/manual-cancel`, {
    reason: reason?.trim() ? reason.trim() : null
  });
}

export async function fetchShipments(limit = 50, offset = 0, searchTerm = ''): Promise<ShipmentView[]> {
  const query = new URLSearchParams({
    limit: String(Math.max(1, limit)),
    offset: String(Math.max(0, offset))
  });

  const normalizedSearch = searchTerm.trim();
  if (normalizedSearch)
  {
    query.set('searchTerm', normalizedSearch);
  }

  return fetchJson(
    `${gatewayUrl()}/api/admin/shipping/v1/shipments?${query.toString()}`
  );
}

export async function fetchShipmentByOrder(orderId: string): Promise<ShipmentView | null> {
  const res = await fetchWithTimeout(`${gatewayUrl()}/api/admin/shipping/v1/shipments/orders/${orderId}`);
  if (res.status === 404) return null;
  if (!res.ok) {
    throw new Error(await getApiErrorMessage(res, 'GET'));
  }

  return res.json();
}

export async function updateShipmentStatus(shipmentId: string, status: ShipmentView['status']): Promise<ShipmentView> {
  return postJson(`${gatewayUrl()}/api/admin/shipping/v1/shipments/${shipmentId}/status`, { status });
}

export async function upsertStock(payload: { productId: string; sku: string; availableQuantity: number }): Promise<void> {
  await postJson(`${gatewayUrl()}/api/admin/warehouse/v1/stock`, payload);
}

export async function fetchWarehouseStockByProducts(
  productIds: string[],
  lowStockThreshold?: number | null
): Promise<WarehouseStockItem[]> {
  const normalizedProductIds = Array.from(new Set(productIds.filter((x) => !!x)));

  if (normalizedProductIds.length === 0) {
    return [];
  }

  const payload = {
    productIds: normalizedProductIds,
    lowStockThreshold:
      typeof lowStockThreshold === 'number' && Number.isFinite(lowStockThreshold)
        ? Math.max(0, Math.trunc(lowStockThreshold))
        : null
  };

  const response = await postJson<{ items?: WarehouseStockItem[] }>(
    `${gatewayUrl()}/api/admin/warehouse/v1/stock/query`,
    payload
  );

  return Array.isArray(response?.items) ? response.items : [];
}

export async function fetchCustomers(limit = 20, offset = 0, searchTerm = ''): Promise<AdminCustomer[]> {
  const query = new URLSearchParams({
    limit: String(Math.max(1, limit)),
    offset: String(Math.max(0, offset))
  });

  const normalizedSearch = searchTerm.trim();
  if (normalizedSearch)
  {
    query.set('searchTerm', normalizedSearch);
  }

  return fetchJson(`${gatewayUrl()}/api/admin/account/v1/customers?${query.toString()}`);
}

export async function fetchCustomer(customerId: string): Promise<AdminCustomer> {
  return fetchJson(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}`);
}

export async function updateCustomer(customerId: string, payload: { firstName: string; lastName: string; phone: string }): Promise<AdminCustomer> {
  return putJson(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}`, payload);
}

export async function resetCustomerPassword(customerId: string, newPassword: string): Promise<void> {
  const response = await fetchWithTimeout(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}/password/reset`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ newPassword })
  });

  if (!response.ok) {
    throw new Error(await getApiErrorMessage(response, 'POST'));
  }
}

export async function fetchCustomerAddresses(customerId: string): Promise<AdminCustomerAddress[]> {
  return fetchJson(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}/addresses`);
}

export async function createCustomerAddress(
  customerId: string,
  payload: Omit<AdminCustomerAddress, 'id'>
): Promise<AdminCustomerAddress> {
  return postJson(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}/addresses`, payload);
}

export async function updateCustomerAddress(
  customerId: string,
  addressId: string,
  payload: Omit<AdminCustomerAddress, 'id'>
): Promise<AdminCustomerAddress> {
  return putJson(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}/addresses/${addressId}`, payload);
}

export async function deleteCustomerAddress(customerId: string, addressId: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/admin/account/v1/customers/${customerId}/addresses/${addressId}`);
}

export async function fetchAdminUsers(limit = 20, offset = 0, searchTerm = ''): Promise<AdminAccountUser[]> {
  const query = new URLSearchParams({
    limit: String(Math.max(1, limit)),
    offset: String(Math.max(0, offset))
  });

  const normalizedSearch = searchTerm.trim();
  if (normalizedSearch)
  {
    query.set('searchTerm', normalizedSearch);
  }

  return fetchJson(`${gatewayUrl()}/api/admin/account/v1/admins?${query.toString()}`);
}

export async function createAdminUser(payload: { username: string; password: string; permissions?: string[] | null }): Promise<AdminAccountUser> {
  return postJson(`${gatewayUrl()}/api/admin/account/v1/admins`, payload);
}

export async function updateAdminUserPermissions(adminUserId: string, permissions: string[] | null): Promise<void> {
  const response = await fetchWithTimeout(`${gatewayUrl()}/api/admin/account/v1/admins/${adminUserId}/permissions`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ permissions })
  });

  if (!response.ok) {
    throw new Error(await getApiErrorMessage(response, 'PUT'));
  }
}

export async function resetAdminUserPassword(adminUserId: string, newPassword: string): Promise<void> {
  const response = await fetchWithTimeout(`${gatewayUrl()}/api/admin/account/v1/admins/${adminUserId}/password/reset`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ newPassword })
  });

  if (!response.ok) {
    throw new Error(await getApiErrorMessage(response, 'POST'));
  }
}

export async function deleteAdminUser(adminUserId: string): Promise<void> {
  await deleteJson(`${gatewayUrl()}/api/admin/account/v1/admins/${adminUserId}`);
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetchWithTimeout(url);
  if (!response.ok) throw new Error(await getApiErrorMessage(response, 'API'));
  return response.json() as Promise<T>;
}

async function postJson<T>(url: string, payload: unknown): Promise<T> {
  const response = await fetchWithTimeout(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!response.ok) {
    throw new Error(await getApiErrorMessage(response, 'POST'));
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
    throw new Error(await getApiErrorMessage(response, 'PUT'));
  }

  return response.json() as Promise<T>;
}

async function deleteJson(url: string): Promise<void> {
  const response = await fetchWithTimeout(url, { method: 'DELETE' });
  if (!response.ok && response.status !== 404) {
    throw new Error(await getApiErrorMessage(response, 'DELETE'));
  }
}

type ApiErrorBody = {
  detail?: string;
  title?: string;
  error?: string;
  message?: string;
  status?: number;
};

async function getApiErrorMessage(response: Response, methodLabel: string): Promise<string> {
  const body = (await response.json().catch(() => null)) as ApiErrorBody | null;

  const detail = body?.detail?.trim();
  const error = body?.error?.trim();
  const message = body?.message?.trim();
  const title = body?.title?.trim();
  const rawMessage = detail || error || message || title || '';

  if (!rawMessage) {
    return `${methodLabel} error: ${response.status}`;
  }

  return normalizeDomainErrorMessage(rawMessage);
}

function normalizeDomainErrorMessage(rawMessage: string): string {
  const cancelMatch = rawMessage.match(/cannot be cancelled from status '([^']+)'/i);
  if (cancelMatch?.[1]) {
    return `Annullamento non consentito: ordine in stato '${cancelMatch[1]}'.`;
  }

  const completeMatch = rawMessage.match(/cannot be completed from status '([^']+)'/i);
  if (completeMatch?.[1]) {
    return `Completamento non consentito: ordine in stato '${completeMatch[1]}'.`;
  }

  return rawMessage;
}

async function fetchWithTimeout(url: string, init?: RequestInit): Promise<Response> {
  let hasRetriedAfterRefresh = false;

  while (true) {
    const response = await fetchWithTimeoutRetry(url, init);
    const canRefresh = typeof window !== 'undefined';

    if (response.status !== 401 || !canRefresh || hasRetriedAfterRefresh) {
      return response;
    }

    hasRetriedAfterRefresh = true;
    const refreshed = await refreshAdminSession();
    if (!refreshed) {
      return response;
    }
  }
}

async function fetchWithTimeoutRetry(url: string, init?: RequestInit): Promise<Response> {
  const shouldRetry = isSafeMethod(init?.method);

  for (let attempt = 0; attempt < (shouldRetry ? 2 : 1); attempt += 1) {
    const controller = new AbortController();
    const timerId = globalThis.setTimeout(() => controller.abort(), requestTimeoutMs);

    try {
      return await fetch(url, {
        ...init,
        headers: withAdminAuthorization(init?.headers),
        signal: controller.signal
      });
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

async function refreshAdminSession(): Promise<boolean> {
  if (typeof window === 'undefined') {
    return false;
  }

  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const response = await fetch(REFRESH_ENDPOINT_PATH, {
          method: 'POST',
          credentials: 'same-origin'
        });

        return response.ok;
      } catch {
        return false;
      } finally {
        refreshInFlight = null;
      }
    })();
  }

  const refreshed = await refreshInFlight;

  if (!refreshed && !loginRedirectTriggered) {
    loginRedirectTriggered = true;
    const nextPath = `${window.location.pathname}${window.location.search}`;
    window.location.assign(`/login?next=${encodeURIComponent(nextPath)}`);
  }

  return refreshed;
}

function withAdminAuthorization(headers?: HeadersInit): Headers {
  const output = new Headers(headers ?? {});
  const token = getAccessTokenFromCookie();
  if (token && !output.has('Authorization')) {
    output.set('Authorization', `Bearer ${token}`);
  }

  return output;
}

function getAccessTokenFromCookie(): string | null {
  if (typeof document === 'undefined') {
    return null;
  }

  const cookie = document.cookie
    .split(';')
    .map((x) => x.trim())
    .find((x) => x.startsWith(`${ACCESS_COOKIE_NAME}=`));

  if (!cookie) {
    return null;
  }

  const raw = cookie.slice(ACCESS_COOKIE_NAME.length + 1);
  if (!raw) {
    return null;
  }

  try {
    return decodeURIComponent(raw);
  } catch {
    return raw;
  }
}

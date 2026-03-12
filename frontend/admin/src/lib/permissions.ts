export const ADMIN_PERMISSION = {
  catalogRead: 'catalog:read',
  catalogWrite: 'catalog:write',
  ordersRead: 'orders:read',
  ordersWrite: 'orders:write',
  shippingRead: 'shipping:read',
  shippingWrite: 'shipping:write',
  warehouseRead: 'warehouse:read',
  warehouseWrite: 'warehouse:write',
  accountRead: 'account:read',
  accountWrite: 'account:write'
} as const;

export type AdminPermission = (typeof ADMIN_PERMISSION)[keyof typeof ADMIN_PERMISSION];

export const ALL_ADMIN_PERMISSIONS: AdminPermission[] = [
  ADMIN_PERMISSION.catalogRead,
  ADMIN_PERMISSION.catalogWrite,
  ADMIN_PERMISSION.ordersRead,
  ADMIN_PERMISSION.ordersWrite,
  ADMIN_PERMISSION.shippingRead,
  ADMIN_PERMISSION.shippingWrite,
  ADMIN_PERMISSION.warehouseRead,
  ADMIN_PERMISSION.warehouseWrite,
  ADMIN_PERMISSION.accountRead,
  ADMIN_PERMISSION.accountWrite
];

export const ADMIN_PERMISSION_LABEL: Record<AdminPermission, string> = {
  [ADMIN_PERMISSION.catalogRead]: 'Catalogo lettura',
  [ADMIN_PERMISSION.catalogWrite]: 'Catalogo scrittura',
  [ADMIN_PERMISSION.ordersRead]: 'Ordini lettura',
  [ADMIN_PERMISSION.ordersWrite]: 'Ordini scrittura',
  [ADMIN_PERMISSION.shippingRead]: 'Spedizioni lettura',
  [ADMIN_PERMISSION.shippingWrite]: 'Spedizioni scrittura',
  [ADMIN_PERMISSION.warehouseRead]: 'Magazzino lettura',
  [ADMIN_PERMISSION.warehouseWrite]: 'Magazzino scrittura',
  [ADMIN_PERMISSION.accountRead]: 'Account lettura',
  [ADMIN_PERMISSION.accountWrite]: 'Account scrittura'
};

type CookieStore = {
  get: (name: string) => { value: string } | undefined;
};

const ACCESS_COOKIE_NAME = 'bo_access_token';

export function getAdminPermissionsFromCookies(cookies: CookieStore): Set<string> {
  const payload = getAdminJwtPayloadFromCookies(cookies);
  return getAdminPermissionsFromPayload(payload);
}

export function isAdminSuperUserFromCookies(cookies: CookieStore): boolean {
  const payload = getAdminJwtPayloadFromCookies(cookies);
  if (!payload) {
    return false;
  }

  const claim = payload.super_user;
  if (typeof claim === 'boolean') {
    return claim;
  }

  if (typeof claim === 'string') {
    return claim.toLowerCase() === 'true';
  }

  return false;
}

export function hasPermission(permissions: Set<string>, permission: AdminPermission): boolean {
  return permissions.has(permission);
}

function getAdminPermissionsFromPayload(payload: Record<string, unknown> | null): Set<string> {
  if (!payload) {
    return new Set();
  }

  const realm = String(payload.realm ?? '');
  if (realm !== 'admin') {
    return new Set();
  }

  const claimPermissions = payload.permission;
  if (Array.isArray(claimPermissions)) {
    return new Set(claimPermissions.filter((x): x is string => typeof x === 'string' && x.length > 0));
  }

  if (typeof claimPermissions === 'string' && claimPermissions.length > 0) {
    return new Set([claimPermissions]);
  }

  return new Set();
}

function getAdminJwtPayloadFromCookies(cookies: CookieStore): Record<string, unknown> | null {
  const token = cookies.get(ACCESS_COOKIE_NAME)?.value;
  return decodeJwtPayload(token);
}

function decodeJwtPayload(token: string | undefined): Record<string, unknown> | null {
  if (!token) return null;

  const chunks = token.split('.');
  if (chunks.length < 2) return null;

  const base64 = chunks[1].replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64 + '='.repeat((4 - (base64.length % 4 || 4)) % 4);

  try {
    const raw = Buffer.from(padded, 'base64').toString('utf8');
    return JSON.parse(raw) as Record<string, unknown>;
  } catch {
    return null;
  }
}
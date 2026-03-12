const ACCESS_COOKIE_NAME = 'bo_access_token';
const REFRESH_COOKIE_NAME = 'bo_refresh_token';

const gatewayUrl = (): string => {
  if (typeof window === 'undefined') {
    return (import.meta.env.GATEWAY_INTERNAL_URL as string | undefined)
      ?? (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined)
      ?? 'http://localhost:18080';
  }

  return (import.meta.env.PUBLIC_GATEWAY_URL as string | undefined) ?? 'http://localhost:18080';
};

type CookieStore = {
  get: (name: string) => { value: string } | undefined;
  set: (name: string, value: string, options?: Record<string, unknown>) => void;
  delete: (name: string, options?: Record<string, unknown>) => void;
};

type LoginResponse = {
  accessToken: string;
  refreshToken: string;
  realm: string;
};

function isValidLoginResponse(body: LoginResponse | null | undefined): body is LoginResponse {
  return !!body?.accessToken && !!body.refreshToken && body.realm === 'admin';
}

export function isAuthenticated(cookies: CookieStore): boolean {
  const token = cookies.get(ACCESS_COOKIE_NAME)?.value;
  if (!token) return false;

  const payload = decodeJwtPayload(token);
  if (!payload) return false;

  const exp = Number(payload.exp ?? 0);
  const realm = String(payload.realm ?? '');
  if (!Number.isFinite(exp) || exp <= 0) return false;
  if (realm !== 'admin') return false;

  const nowSeconds = Math.floor(Date.now() / 1000);
  return exp > nowSeconds;
}

export async function validateCredentials(username: string, password: string): Promise<LoginResponse | null> {
  const res = await fetch(`${gatewayUrl()}/api/admin/account/v1/users/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });

  if (!res.ok) {
    return null;
  }

  const body = await res.json() as LoginResponse;
  if (!isValidLoginResponse(body)) {
    return null;
  }

  return body;
}

export async function refreshSession(cookies: CookieStore): Promise<boolean> {
  const refreshToken = getRefreshToken(cookies);
  if (!refreshToken) {
    clearAuthCookie(cookies);
    return false;
  }

  const response = await fetch(`${gatewayUrl()}/api/admin/account/v1/users/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  }).catch(() => null);

  if (!response?.ok) {
    clearAuthCookie(cookies);
    return false;
  }

  const body = await response.json() as LoginResponse;
  if (!isValidLoginResponse(body)) {
    clearAuthCookie(cookies);
    return false;
  }

  setAuthCookie(cookies, body);
  return true;
}

export async function revokeSession(refreshToken: string | undefined): Promise<void> {
  if (!refreshToken) return;

  await fetch(`${gatewayUrl()}/api/admin/account/v1/users/logout`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken })
  }).catch(() => undefined);
}

export function setAuthCookie(cookies: CookieStore, login: LoginResponse): void {
  cookies.set(ACCESS_COOKIE_NAME, login.accessToken, {
    path: '/',
    // Access token is used by browser-side admin API calls.
    httpOnly: false,
    sameSite: 'lax',
    secure: import.meta.env.PROD,
    maxAge: 60 * 60
  });

  cookies.set(REFRESH_COOKIE_NAME, login.refreshToken, {
    path: '/',
    httpOnly: true,
    sameSite: 'lax',
    secure: import.meta.env.PROD,
    maxAge: 60 * 60 * 24 * 14
  });
}

export function clearAuthCookie(cookies: CookieStore): void {
  cookies.delete(ACCESS_COOKIE_NAME, { path: '/' });
  cookies.delete(REFRESH_COOKIE_NAME, { path: '/' });
}

export function getRefreshToken(cookies: CookieStore): string | undefined {
  return cookies.get(REFRESH_COOKIE_NAME)?.value;
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
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

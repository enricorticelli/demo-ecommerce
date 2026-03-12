import { defineMiddleware } from 'astro:middleware';
import { isAuthenticated } from './lib/auth';
import { ADMIN_PERMISSION, getAdminPermissionsFromCookies, isAdminSuperUserFromCookies } from './lib/permissions';

const PUBLIC_PATH_PREFIXES = ['/login', '/api/auth/refresh'];

const READ_PERMISSION_BY_PATH_PREFIX: Array<{ prefix: string; permission: string }> = [
  { prefix: '/catalog', permission: ADMIN_PERMISSION.catalogRead },
  { prefix: '/orders', permission: ADMIN_PERMISSION.ordersRead },
  { prefix: '/shipments', permission: ADMIN_PERMISSION.shippingRead },
  { prefix: '/warehouse', permission: ADMIN_PERMISSION.warehouseRead },
  { prefix: '/customers', permission: ADMIN_PERMISSION.accountRead },
  { prefix: '/users', permission: ADMIN_PERMISSION.accountRead },
  { prefix: '/admin-users', permission: ADMIN_PERMISSION.accountRead }
];

export const onRequest = defineMiddleware((context, next) => {
  const pathname = context.url.pathname;

  const isPublicPath = PUBLIC_PATH_PREFIXES.some((prefix) => pathname.startsWith(prefix));
  if (isPublicPath) {
    return next();
  }

  if (!isAuthenticated(context.cookies)) {
    const redirectParams = new URLSearchParams();
    if (pathname !== '/') {
      redirectParams.set('next', pathname);
    }

    const queryString = redirectParams.toString();
    const redirectPath = queryString.length > 0
      ? `/login?${queryString}`
      : '/login';

    return context.redirect(redirectPath);
  }

  const requiredPermission = READ_PERMISSION_BY_PATH_PREFIX
    .find((entry) => pathname.startsWith(entry.prefix))
    ?.permission;

  if (requiredPermission) {
    const permissions = getAdminPermissionsFromCookies(context.cookies);
    if (!permissions.has(requiredPermission)) {
      return context.redirect('/');
    }
  }

  if (pathname.startsWith('/users') || pathname.startsWith('/admin-users')) {
    const isSuperUser = isAdminSuperUserFromCookies(context.cookies);
    if (!isSuperUser) {
      return context.redirect('/');
    }
  }

  return next();
});

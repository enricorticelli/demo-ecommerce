import { defineMiddleware } from 'astro:middleware';
import { isAuthenticated } from './lib/auth';

const PUBLIC_PATH_PREFIXES = ['/login'];

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

  return next();
});

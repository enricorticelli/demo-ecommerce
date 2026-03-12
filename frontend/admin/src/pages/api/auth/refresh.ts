import type { APIRoute } from 'astro';
import { refreshSession } from '../../../../lib/auth';

export const POST: APIRoute = async ({ cookies }) => {
  const refreshed = await refreshSession(cookies);
  if (!refreshed) {
    return new Response(null, { status: 401 });
  }

  return new Response(null, { status: 204 });
};

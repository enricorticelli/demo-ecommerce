import { User, UserManager, WebStorageStateStore } from 'oidc-client-ts';

import { getBackofficeConfig } from '../config/runtimeConfig';

const config = getBackofficeConfig();
const appOrigin = window.location.origin;

export const userManager = new UserManager({
  authority: config.keycloakAuthority,
  client_id: config.keycloakClientId,
  redirect_uri: `${appOrigin}/`,
  post_logout_redirect_uri: appOrigin,
  response_type: 'code',
  scope: 'openid',
  userStore: new WebStorageStateStore({ store: window.sessionStorage }),
});

export async function getCurrentUser(): Promise<User | null> {
  const user = await userManager.getUser();

  if (!user || user.expired) {
    return null;
  }

  return user;
}

export async function getAccessToken(): Promise<string | null> {
  const user = await getCurrentUser();
  return user?.access_token ?? null;
}

export async function completeSignInIfNeeded(): Promise<void> {
  const hasCode = new URLSearchParams(window.location.search).has('code');
  const hasState = new URLSearchParams(window.location.search).has('state');
  if (!hasCode || !hasState) {
    return;
  }

  await userManager.signinRedirectCallback();
  window.history.replaceState({}, document.title, window.location.pathname);
}

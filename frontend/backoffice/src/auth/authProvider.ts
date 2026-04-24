import type { AuthProvider } from 'react-admin';

import { getCurrentUser, userManager } from './keycloak';

export const authProvider: AuthProvider = {
  login: async () => {
    await userManager.signinRedirect();
  },
  logout: async () => {
    const user = await getCurrentUser();
    await userManager.removeUser();
    if (user?.id_token) {
      await userManager.signoutRedirect({ post_logout_redirect_uri: window.location.origin });
    }
  },
  checkAuth: async () => {
    const user = await getCurrentUser();
    if (!user) {
      throw new Error('Authentication required.');
    }
  },
  checkError: async (error) => {
    const status = error?.status;
    if (status === 401 || status === 403) {
      await userManager.removeUser();
      throw new Error('Authentication required.');
    }
  },
  getIdentity: async () => {
    const user = await getCurrentUser();
    if (!user) {
      throw new Error('Authentication required.');
    }

    return {
      id: user.profile.sub ?? 'backoffice-user',
      fullName: 'Backoffice user',
    };
  },
  getPermissions: async () => null,
};

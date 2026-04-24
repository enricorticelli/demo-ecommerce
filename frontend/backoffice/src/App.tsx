import { CreateGuesser, EditGuesser, ListGuesser, OpenApiAdmin, openApiDataProvider } from '@api-platform/admin';
import type { HttpClientOptions } from '@api-platform/admin';
import { Resource, fetchUtils } from 'react-admin';

import { authProvider } from './auth/authProvider';
import { LoginPage } from './auth/LoginPage';
import { getAccessToken } from './auth/keycloak';
import { getBackofficeConfig } from './config/runtimeConfig';
import { createBackofficeDataProvider } from './dataProvider';

const config = getBackofficeConfig();

async function authenticatedHttpClient(url: URL, options: HttpClientOptions = {}) {
  const token = await getAccessToken();
  const rawHeaders = typeof options.headers === 'function' ? options.headers() : options.headers;
  const headers = new Headers(rawHeaders);

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  return fetchUtils.fetchJson(url.toString(), {
    ...options,
    headers,
  } as fetchUtils.Options);
}

const dataProvider = openApiDataProvider({
  entrypoint: config.apiEntrypoint,
  docEntrypoint: config.openApiUrl,
  dataProvider: createBackofficeDataProvider(config.apiEntrypoint),
  httpClient: authenticatedHttpClient,
});

export function App() {
  return (
    <OpenApiAdmin
      entrypoint={config.apiEntrypoint}
      docEntrypoint={config.openApiUrl}
      dataProvider={dataProvider}
      authProvider={authProvider}
      loginPage={LoginPage}
      requireAuth
      title="E-commerce Backoffice">
      <Resource name="products" list={ListGuesser} edit={EditGuesser} create={CreateGuesser} />
      <Resource name="brands" list={ListGuesser} edit={EditGuesser} create={CreateGuesser} />
      <Resource name="categories" list={ListGuesser} edit={EditGuesser} create={CreateGuesser} />
      <Resource name="collections" list={ListGuesser} edit={EditGuesser} create={CreateGuesser} />
    </OpenApiAdmin>
  );
}

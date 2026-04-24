export type BackofficeConfig = {
  apiEntrypoint: string;
  openApiUrl: string;
  keycloakAuthority: string;
  keycloakClientId: string;
};

const defaults: BackofficeConfig = {
  apiEntrypoint: 'http://localhost:18080',
  openApiUrl: 'http://localhost:18080/openapi/backoffice.json',
  keycloakAuthority: 'http://localhost:18081/realms/demo-ecommerce',
  keycloakClientId: 'backoffice-web',
};

function trimTrailingSlash(value: string): string {
  return value.replace(/\/+$/, '');
}

export function getBackofficeConfig(): BackofficeConfig {
  const runtime = window.__BACKOFFICE_CONFIG__ ?? {};

  return {
    apiEntrypoint: trimTrailingSlash(import.meta.env.VITE_BACKOFFICE_API_ENTRYPOINT ?? runtime.apiEntrypoint ?? defaults.apiEntrypoint),
    openApiUrl: import.meta.env.VITE_BACKOFFICE_OPENAPI_URL ?? runtime.openApiUrl ?? defaults.openApiUrl,
    keycloakAuthority: trimTrailingSlash(import.meta.env.VITE_KEYCLOAK_AUTHORITY ?? runtime.keycloakAuthority ?? defaults.keycloakAuthority),
    keycloakClientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? runtime.keycloakClientId ?? defaults.keycloakClientId,
  };
}

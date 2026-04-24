/// <reference types="vite/client" />

type BackofficeRuntimeConfig = {
  apiEntrypoint?: string;
  openApiUrl?: string;
  keycloakAuthority?: string;
  keycloakClientId?: string;
};

interface Window {
  __BACKOFFICE_CONFIG__?: BackofficeRuntimeConfig;
}

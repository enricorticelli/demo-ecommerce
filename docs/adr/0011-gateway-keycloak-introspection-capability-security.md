# ADR-0011: Gateway Authentication with Keycloak Introspection and Capabilities

- Status: accepted
- Date: 2026-04-24

## Context

Backoffice APIs need authentication and least-privilege authorization without storing application users in repository configuration files. Client-visible tokens must not contain profile data or full capability lists.

The Gateway is the single public HTTP aggregation point (ADR-0010), so it is the right boundary for request authentication before reverse proxying to bounded contexts.

## Decision

Use Keycloak as the source of truth for backoffice users, credentials, and capability assignments. The Gateway accepts bearer tokens but treats them as opaque: it calls Keycloak's token introspection endpoint to validate token activity and resolve server-side capabilities.

- Backoffice routes under `/api/backoffice/**` require Gateway authorization policies.
- Storefront routes, payment webhooks, health checks, system info, and OpenAPI remain public.
- Capability roles are excluded from client-visible access tokens and included only in introspection responses.
- Gateway authorization evaluates capability claims returned by introspection, supporting exact grants, namespace wildcards such as `catalog.*`, and the global `*` grant.
- Introspection responses may be cached briefly by the Gateway, never beyond token expiry.

Local development includes a Keycloak realm import with clients and capability roles only. Application users and role assignments are created in Keycloak, not in app configuration files.

## Consequences

- Clients cannot rely on token contents for permissions.
- Capability decisions are centralized at the Gateway while bounded contexts remain independent.
- Keycloak availability is required for uncached authorization checks.
- Production deployments must provide Keycloak URL and Gateway introspection client credentials through environment variables or a secret manager.

## References

- Keycloak lightweight access tokens and token introspection mappers: https://www.keycloak.org/docs/latest/server_admin/
- Keycloak token introspection endpoint API: https://www.keycloak.org/docs-api/latest/javadocs/org/keycloak/protocol/oidc/endpoints/TokenIntrospectionEndpoint.html

# HTTP Endpoint Conventions

## Goal

Define a single standard for naming, paths, versioning, and `store`/`backoffice` context separation.

## Base pattern

1. All public endpoints must pass through the gateway.
2. Required public path format:
   - `/api/{context}/{service}/v{version}/...`
3. Allowed `context` values:
   - `store`
   - `backoffice`
4. `service` matches the exposed bounded context (`catalog`, `cart`, `order`, `payment`, `shipping`, `warehouse`).

## Versioning

1. Version is always in the path (`v1`, `v2`, ...).
2. By default, evolve the current version only with backward-compatible changes.
3. Breaking changes follow ADR-0004.
4. Registered active exception: `store`/`backoffice` separation with direct cut-over on `v1` (see ADR-0004).

## Exposure rules

1. Strict whitelist in the gateway: expose only explicitly authorized endpoints.
2. No public legacy routes without context (`/api/{service}/...`).
3. Every endpoint must clearly belong to either `store` or `backoffice`, based on the consumer.
4. If one handler is available in both contexts, endpoint names must remain unique.
5. Full management CRUD (list/get/create/update/delete) must be exposed under `backoffice`.
6. `store` must expose only endpoints required by the customer storefront journey.

## Endpoint naming conventions

1. Use resource-oriented paths with plural names (`/products`, `/orders`, `/shipments`).
2. Use non-CRUD actions only when necessary, with explicit verb in the final segment:
   - `/manual-cancel`
   - `/manual-complete`
3. No domain logic in endpoint mappers.

## Query params and payloads

1. Query params are allowed only for filtering, searching, and pagination.
2. Query/payload fields must use `camelCase`.
3. Validate input at the API boundary before delegating to application services.

## Response codes

1. `200` for reads/updates with body.
2. `201` for create with location aligned to the contextualized path.
3. `204` for delete without body.
4. `400`/`404`/`409` through shared error mapping.

## PR checklist for new endpoints

1. Path follows `/api/{context}/{service}/v{version}/...`.
2. Endpoint added to gateway whitelist.
3. Request/response contract is explicit and tested.
4. Consumers updated (`frontend/web`, backoffice tools/scripts) if impacted.
5. Documentation updated (`README`, `docs/`, ADR if needed).

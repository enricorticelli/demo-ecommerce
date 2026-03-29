# Bounded Context: Catalog

## Purpose

Manage the product catalog and commercial metadata (brands, categories, collections).

## Responsibilities

1. Define and maintain product records.
2. Expose product listing and detail queries.
3. Guarantee internal catalog data consistency.

## Data ownership

- Products.
- Brands.
- Categories.
- Collections.

## Integrations

- Exposes APIs through the gateway in two contexts:
  - `store`: storefront-facing read endpoints for user experience.
  - `backoffice`: management APIs with full CRUD for products, brands, categories, collections.
- Publishes events when relevant catalog data changes for other contexts.

## Public endpoints (summary)

- `store`:
  - `GET /api/store/catalog/v1/products/new-arrivals`
  - `GET /api/store/catalog/v1/products/best-sellers`
- `backoffice`:
  - `GET/POST /api/backoffice/catalog/v1/products`
  - `GET/PUT/DELETE /api/backoffice/catalog/v1/products/{id}`
  - `GET/POST /api/backoffice/catalog/v1/brands`
  - `GET/PUT/DELETE /api/backoffice/catalog/v1/brands/{id}`
  - `GET/POST /api/backoffice/catalog/v1/categories`
  - `GET/PUT/DELETE /api/backoffice/catalog/v1/categories/{id}`
  - `GET/POST /api/backoffice/catalog/v1/collections`
  - `GET/PUT/DELETE /api/backoffice/catalog/v1/collections/{id}`

## Adopted implementation conventions

1. Thin API endpoints: orchestration delegated to command/query services.
2. Centralized `View -> Response` mapping in static API mappers.
3. Separate EF Core repositories per aggregate (`Brand`, `Category`, `Collection`, `Product`).
4. Cross-entity rules in application `Rules` (uniqueness, references, delete constraints).
5. Server-side `searchTerm` support in dedicated query components.
6. Event publishing via `IDomainEventPublisher` with outbox adapter in infrastructure.
7. `*V1` events in `Shared.BuildingBlocks.Contracts.IntegrationEvents.Catalog` with standard metadata.

## Boundaries

No other context can read/write the catalog database directly.

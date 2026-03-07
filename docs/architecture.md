# Architecture Overview

This repository implements a CQRS + Event Sourcing e-commerce baseline with .NET 10 Minimal APIs, Wolverine, Marten, RabbitMQ, PostgreSQL, and a YARP gateway.

## Scope
- Docker-first runtime (`docker compose up --build`).
- Core flow: catalog -> cart -> order -> stock -> payment -> shipping -> final order state.
- No authentication/identity in this phase.

## Core principles
- CQRS (write/read separation).
- CQRS puro in tutti i moduli backend (command/query + handler + dispatcher).
- Event-driven orchestration for checkout.
- No direct HTTP service-to-service in checkout workflow.
- Event sourcing for `Cart` and `Order`.
- Data-per-service (schema-per-service).
- SOLID and dependency inversion across backend layers.

## Services
- `Catalog.Api`: products CRUD.
- `Cart.Api`: cart write/read on event stream.
- `Order.Api`: checkout orchestration and order state.
- `Warehouse.Api`: stock reservation and stock events.
- `Payment.Api`: payment authorization simulation.
- `Shipping.Api`: shipment creation and tracking.
- `Shipping.Api`: shipment creation, tracking, and operational status management.
- `User.Api`: demo user profile read.
- `Gateway.Api`: simple YARP reverse proxy.
- `frontend/web`: public storefront.
- `frontend/admin`: separated backoffice frontend.

## Integration event flow
1. `OrderPlacedV1`
2. `StockReservedV1` or `StockRejectedV1`
3. `PaymentAuthorizeRequestedV1`
4. `PaymentAuthorizedV1` or `PaymentFailedV1`
5. `ShippingCreateRequestedV1`
6. `ShippingCreatedV1`
7. `OrderCompletedV1` or `OrderFailedV1`

Additional workflow integration:
- `OrderCompletedV1` is consumed by `Cart` to close the source cart and create a new empty cart.

## Operational backoffice notes
- Backoffice includes a dedicated Shipping section for operational lifecycle updates (`Preparing`, `Created`, `InTransit`, `Delivered`, `Cancelled`).
- A controlled manual override is supported for order management: from backoffice, `Completed` orders can be manually cancelled (`Failed`) as an explicit operator action.

## Technical governance
- Detailed technical guidelines and implementation rules:
  - `docs/technical-guidelines.md`
- API request/response/mapper conventions:
  - `docs/api-patterns.md`
- Checkout flow (detailed sequence/state diagrams):
  - `docs/checkout-flow.md`
- Architectural decisions (ADR):
  - `docs/adr/0001-wolverine-marten.md`
  - `docs/adr/0002-rabbitmq.md`
  - `docs/adr/0003-frontend-astro-svelte.md`
  - `docs/adr/0004-clean-architecture-cqrs-template.md`
  - `docs/adr/0005-mongodb-read-models.md`
  - `docs/adr/0006-separated-backoffice-frontend.md`

## Recent changes (latest commits)

### `896ab3d` - `feat: aggiunge backoffice`
- Introduced dedicated backoffice app (`frontend/admin`) on separate port.
- Moved catalog management out of public storefront.
- Added admin pages for catalog, orders list, order detail, warehouse operations.
- Added order list query endpoint and payment sessions list query endpoint for operations UI.

### `c5bbcaa` - `feat: chiude giro checkout e magazzino`
- Completed checkout + warehouse flow with stronger orchestration path.
- Introduced MongoDB read models for cart and order query side.
- Added shared read model base abstractions in `Shared.BuildingBlocks`.
- Added stock upsert API and extended seed script to prepare purchasable data.

### `1c1a19e` - `refactor: clean architecture`
- Standardized module layering into `Api/Application/Domain/Infrastructure`.
- Introduced shared CQRS abstractions/dispatchers/pipeline behaviors in `Shared.BuildingBlocks`.
- Moved endpoint logic to command/query handlers and reduced Program.cs wiring complexity.
- Added payment session hosted flow page in storefront.

### `32c28d1` - `feat: catalogo + seed`
- Expanded catalog surface (products, brands, categories, collections).
- Introduced deterministic seed-and-smoke script driven by public APIs.
- Added initial integration contracts for checkout workflow events.

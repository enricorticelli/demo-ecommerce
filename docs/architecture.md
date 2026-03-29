# Target Backend Architecture

## Goal

Build a non-mock backend based on pragmatic microservices, aligned with domain bounded contexts and sustainable for a small team.

## Bounded contexts

- `Catalog`: product catalog and commercial metadata.
- `Cart`: cart management and pre-order state.
- `Order`: order lifecycle and process orchestration.
- `Payment`: payment authorization and status.
- `Shipping`: shipment creation and progress.
- `Warehouse`: stock availability and reservation.
- `Communication`: event-driven external communications (email).
- `Account`: customer/backoffice identity and profile.
- `Gateway`: HTTP routing, no domain logic.

## Integration model

1. Synchronous HTTP through the gateway for user commands and queries.
2. Asynchronous event-driven communication for cross-context processes.
3. No direct database access across contexts.
4. Explicit, versioned integration contracts.

## Public HTTP contexts

1. `store`: endpoints for storefront/customer journey (`/api/store/{service}/v1/...`).
2. `backoffice`: operational/management endpoints (`/api/backoffice/{service}/v1/...`).
3. Full management CRUD belongs to `backoffice`; `store` exposes only customer-facing APIs.

## Consistency and transactions

- Strong consistency only inside a single bounded context.
- Cross-context consistency is handled as eventual consistency.
- Every cross-context flow must include retry, idempotency, and compensation.

## Operational constraints

- Single team: prioritize simplicity and repeatable standards.
- High time-to-market pressure: use a vertical-slice roadmap.
- Medium/high load expectations: data isolation and minimum observability are mandatory.

## Architecture acceptance criteria

1. No direct dependencies between domain models of different contexts.
2. Every cross-context integration uses contract-based APIs or events.
3. Every non-trivial decision is tracked in an ADR.
4. Every new flow has domain, integration, and contract tests.

## Implementation reference conventions

1. Each module follows `Api/Application/Domain/Infrastructure` separation.
2. `Api` contains only endpoints, contracts, and static `View -> Response` mappers.
3. `Application` separates `CommandService` and `QueryService` with dedicated repositories/rules/mappers.
4. `Infrastructure` implements technical adapters (DB, broker, outbox) and repositories.
5. Shared integration events live in `Shared.BuildingBlocks.Contracts.IntegrationEvents`.

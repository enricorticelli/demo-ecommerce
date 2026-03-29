# Backend Implementation Roadmap (From Mock to Real)

## Principle

Evolve incrementally by vertical slice while keeping the system always deployable.

## Phase 1: Foundations

1. Align missing namespaces/types to make existing tests buildable.
2. Introduce separated command/query services and module-level repositories/rules/mappers.
3. Remove stub responses from critical paths.
4. Adopt module baseline conventions (`module-baseline-conventions.md`) for new modules.

## Phase 2: Core order flow

1. `Order`: order creation with domain invariants.
2. `Payment`: real payment authorization.
3. `Shipping`: event-driven shipment creation.
4. `Warehouse`: stock reservation and workflow response.

## Phase 3: Hardening

1. Consumer idempotency and deduplication.
2. Retry policies and transient error handling.
3. Observability: correlation id, key metrics, tracing.

## Phase 4: Functional extension

1. Full non-mock catalog CRUD in `backoffice` context.
2. Persistent cart with robust checkout.
3. Improved query/read model for frontend.
4. Apply catalog-derived conventions to `Order`, `Payment`, `Shipping`, `Warehouse`.

## Exit criteria per phase

1. Build green.
2. Unit and integration tests green for introduced flows.
3. API/event contracts documented and compatible.
4. No critical endpoint returning fake data.

## Related ADRs

- `../adr/0001-pragmatic-microservices.md`
- `../adr/0010-inter-context-communication-events-only.md`
- `../adr/0003-data-ownership-separate-databases.md`
- `../adr/0004-contract-first-versioning.md`
- `../adr/0005-eventual-consistency-compensations.md`
- `../adr/0006-idempotency-deduplication.md`
- `../adr/0007-minimum-distributed-observability.md`
- `../adr/0008-backend-test-strategy.md`

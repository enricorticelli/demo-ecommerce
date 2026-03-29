# Integration Events Guidelines

## Goal

Define clear, versioned, and resilient integration events for communication between bounded contexts.

## Design rules

1. An event represents a business fact that already happened.
2. Event names use past tense (`OrderPlacedV1`, `PaymentAuthorizedV1`).
3. Payload contains only what consumers need.
4. No dependency on internal entities of other contexts.
5. Contracts are defined in `Shared.BuildingBlocks.Contracts.IntegrationEvents.<Context>`.
6. One event type per file.

## Versioning

1. Every breaking change requires a new event version.
2. Prefer backward-compatible extensions whenever possible.
3. Deprecate old versions with an explicit plan.

## Required metadata

1. `eventId` unique.
2. `occurredAtUtc`.
3. `correlationId` for end-to-end tracing.
4. `sourceContext`.

## Consumer policy

1. Handlers must be idempotent by default.
2. Use retry with backoff for transient errors.
3. Use dead-letter queues and a replay runbook.
4. Use structured logging with correlation id.

## Producer policy

1. `Application` depends only on `IDomainEventPublisher`.
2. Technical implementation (for example Wolverine outbox) stays in `Infrastructure`.
3. Event publication and state persistence must happen in the same transactional boundary.

## Contract testing

1. Every producer publishes event schema/contract.
2. Every consumer validates compatibility before deployment.

## Related ADRs

- `../adr/0010-inter-context-communication-events-only.md`
- `../adr/0003-data-ownership-separate-databases.md`
- `../adr/0004-contract-first-versioning.md`
- `../adr/0005-eventual-consistency-compensations.md`
- `../adr/0006-idempotency-deduplication.md`

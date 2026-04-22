# ADR-0006: Idempotency and Deduplication in Message Consumers

- Status: accepted
- Date: 2026-04-22

## Context

RabbitMQ provides at-least-once delivery. Without deduplication, a redelivered event (after a network hiccup or consumer crash) may apply the same state change twice.

## Decision

Integration event handlers track processed event IDs in a deduplication store before processing.

- The abstraction `ICartEventDeduplicationStore` (`src/Cart.Application/Abstractions/Idempotency/`) exposes a `HasBeenProcessedAsync` / `MarkAsProcessedAsync` contract.
- The infrastructure implementation `InMemoryCartEventDeduplicationStore` (`src/Cart.Infrastructure/Idempotency/`) is used in development; production-grade contexts should use a persistent store.
- Wolverine's PostgreSQL message persistence (`PersistMessagesWithPostgresql`) also contributes outbox-level deduplication on the producer side (`CartHostBuilderExtensions.cs` line 35).
- `IntegrationEventMetadata` (via `IntegrationEventMetadataFactory`) carries the event ID used for deduplication.

## Consequences

- Handlers are safe to replay without side effects.
- The in-memory store is lost on restart; a database-backed store is needed for production.
- Every new consumer must implement or reuse the deduplication check.

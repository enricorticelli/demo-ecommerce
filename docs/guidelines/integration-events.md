# G-EVENTS: Integration Event Conventions

- Status: active

## Naming

- Event type names follow the pattern `[Subject][Verb]V[N]` where N is the version number, e.g., `ProductCreatedV1`, `OrderCompletedV1`, `StockReservedV1`.
- Events are immutable C# records inheriting `IntegrationEventBase`.
- All event contracts live in `src/Shared.BuildingBlocks/Contracts/IntegrationEvents/[Context]/`.

## Event Metadata

- Every event carries `IntegrationEventMetadata` (correlation ID, causation ID, timestamp) produced by `IntegrationEventMetadataFactory`.
- Never strip or omit metadata; it is required for deduplication and tracing (ADR-0006, ADR-0007).

## Versioning

- Breaking changes (removed fields, renamed fields, type changes) require a new event version record (`V2`, etc.).
- Old and new versions coexist until all consumers are migrated (ADR-0004).
- Do not rename or change the type of an existing event record field.

## Publishing

- Publish via `IDomainEventPublisher.PublishAndFlushAsync()` or `PublishBatchAndFlushAsync()` from the application layer only.
- The Wolverine outbox ensures atomicity with the database transaction (`OutboxDomainEventPublisher`, `src/Catalog.Infrastructure/Messaging/OutboxDomainEventPublisher.cs`).

## Consuming (Handlers)

- Handlers are Wolverine message handler classes in `[Context].Application/Handlers/`.
- Every handler must check the deduplication store before processing to handle at-least-once delivery (ADR-0006).
- Handlers must not call another bounded context's HTTP API (ADR-0010).

## RabbitMQ Topology

- Fanout exchanges are used for events consumed by multiple services (e.g., `order-completed` fanout, `CartHostBuilderExtensions.cs` line 26).
- Direct queues are used for service-specific routing (e.g., `catalog-product-updated-cart`, line 28).
- Queue and exchange names follow the pattern `[subject]-[verb]-[consumer]` in kebab-case.

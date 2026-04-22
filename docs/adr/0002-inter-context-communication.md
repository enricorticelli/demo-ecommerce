# ADR-0002: Inter-Context Communication via RabbitMQ and Wolverine

- Status: accepted
- Date: 2026-04-22

## Context

Bounded contexts must exchange information without compile-time coupling. Synchronous HTTP calls between services create tight temporal coupling and cascading failure risk.

## Decision

All inter-context communication uses **asynchronous integration events** published to RabbitMQ and consumed via the **Wolverine Fx** messaging framework (v5.14.0, `Directory.Packages.props` lines 14–17).

- Events are declared as C# records in `src/Shared.BuildingBlocks/Contracts/IntegrationEvents/` and versioned with a `V1` suffix.
- Producers publish via `IDomainEventPublisher` / `OutboxDomainEventPublisher` (see `src/Catalog.Infrastructure/Messaging/OutboxDomainEventPublisher.cs`).
- Consumers implement Wolverine message handlers registered via `[Context]HostBuilderExtensions`.
- RabbitMQ queue and exchange topology is configured per service (e.g., `CartHostBuilderExtensions.cs` lines 26–33).

## Consequences

- Services communicate without HTTP calls between bounded contexts.
- Delivery is guaranteed by the Wolverine outbox (see ADR-0005).
- Any new event requires a shared contract record in `Shared.BuildingBlocks.Contracts`.

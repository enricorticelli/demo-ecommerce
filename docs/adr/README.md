# ADR Index

This folder stores Architecture Decision Records.

## Index

- [ADR-0000 Repository decision policy](./0000-template.md)
- [ADR-0001 Pragmatic microservices via bounded contexts](./0001-pragmatic-microservices.md)
- [ADR-0002 Inter-context communication via RabbitMQ and Wolverine](./0002-inter-context-communication.md)
- [ADR-0003 Data ownership — separate database per bounded context](./0003-data-ownership-separate-databases.md)
- [ADR-0004 Contract-first versioning of integration events](./0004-contract-first-versioning.md)
- [ADR-0005 Eventual consistency and compensation via event choreography](./0005-eventual-consistency-compensations.md)
- [ADR-0006 Idempotency and deduplication in message consumers](./0006-idempotency-deduplication.md)
- [ADR-0007 Minimum distributed observability via OpenTelemetry and Aspire Dashboard](./0007-minimum-distributed-observability.md)
- [ADR-0008 Backend test strategy — unit, integration, and architecture tests](./0008-backend-test-strategy.md)
- [ADR-0009 Event-driven email via a dedicated Communication service](./0009-event-driven-email-communication-service.md)
- [ADR-0010 No synchronous HTTP between bounded contexts](./0010-inter-context-communication-events-only.md)

## ADR Status Rules

Use one of these statuses:
- proposed
- accepted
- superseded
- deprecated

Create ADRs for decisions about:
- architecture
- persistence
- integrations
- security
- testing strategy

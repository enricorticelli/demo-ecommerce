# Glossary

Domain terms used in demo-ecommerce.

---

## Aggregate

A cluster of domain objects treated as a single unit for data changes. Examples: `Order` (owns `OrderItem`s, `OrderAddress`, `OrderCustomer`). State changes go through the aggregate root's methods only. See `src/Order.Domain/Entities/Order.cs`.

## AnonymousId

A client-generated GUID that identifies a guest user session before authentication. An order may be created by an anonymous user and later claimed via `Order.ClaimByAuthenticatedUser()`.

## Bounded Context

An autonomous service that owns a specific business subdomain, its domain model, its database, and its API. Contexts in this project: Catalog, Cart, Order, Payment, Warehouse, Shipping, Communication.

## Cart

A temporary collection of `CartItem`s associated with a user (authenticated or anonymous) before checkout. Managed by the Cart bounded context. Cleared on `OrderCompletedV1`.

## CommunicationEmailMessage

A domain entity in the Communication context representing a transactional email to be sent (e.g., order confirmation). Persisted before sending to enable retry.

## Correlation ID

A unique identifier propagated across all services and log entries for a single user-initiated request or event chain. Carried in `IntegrationEventMetadata` and W3C trace context headers.

## Domain Event / Integration Event

A **domain event** is a fact that happened within a bounded context. An **integration event** is a domain event published to other bounded contexts via RabbitMQ. All integration events inherit `IntegrationEventBase` and are versioned (e.g., `OrderCreatedV1`).

## Eventual Consistency

The property where all bounded contexts converge to a consistent state after all events have been processed, without requiring a distributed transaction. See ADR-0005.

## Fanout Exchange

A RabbitMQ exchange that delivers a copy of each message to all bound queues. Used for events consumed by multiple services (e.g., `order-completed` fanout, consumed by Cart and Communication). Configured via Wolverine in `[Context]HostBuilderExtensions`.

## Gateway

The `Gateway.Api` service acting as a YARP reverse proxy. It aggregates all bounded context routes under a single public URL and composes a unified OpenAPI document.

## IdentityType

A string on `Order` indicating whether the buyer is `Registered` (authenticated user) or `Anonymous` (guest). Affects ownership and later claim flow.

## Integration Event Metadata

Structured data attached to every integration event: correlation ID, causation ID, event ID (for deduplication), and timestamp. Produced by `IntegrationEventMetadataFactory`.

## Mailpit

A local SMTP mock server used in development to capture outbound emails without actual delivery. Accessible at the `MAILPIT_UI_HOST_PORT`.

## Order

The primary aggregate of the Order bounded context. Tracks payment authorisation and stock reservation flags and transitions through `Pending → Completed | Cancelled`. See `src/Order.Domain/Entities/Order.cs`.

## OrderStatus

Enum with values: `Pending`, `Completed`, `Cancelled`. Transitions are enforced inside `Order` domain methods.

## Outbox Pattern

A technique where integration events are written to a database table atomically with the domain change, then forwarded to the message broker by a background process. Implemented via Wolverine's `IDbContextOutbox` and `OutboxDomainEventPublisher`.

## PaymentSession

A domain entity in the Payment context representing an in-progress checkout session with a payment provider (Stripe, PayPal, Satispay).

## Saga / Choreography

The order fulfilment flow is a choreographed saga: each bounded context reacts to events and emits new events without a central orchestrator. See ADR-0005.

## Shared.BuildingBlocks

A shared C# project (`src/Shared.BuildingBlocks/`) containing cross-cutting concerns: integration event contracts, API defaults, observability registration, exception types, and messaging abstractions. Must stay thin — no business logic.

## Stock Reservation

An act by the Warehouse context that temporarily holds inventory for an order. Communicated via `StockReservedV1` / `StockRejectedV1`.

## TrackingCode

A shipping tracking identifier assigned to an `Order` when it is completed, sourced from the Shipping context.

## TransactionId

The identifier returned by the payment provider (Stripe, PayPal, Satispay) confirming a successful payment authorisation. Stored on the `Order` aggregate.

## Value Object

An immutable domain concept with no identity, defined by its attributes. Examples: `OrderAddress`, `OrderCustomer`, `OrderItem`. Represented as C# records.

## WarehouseReservation

A domain entity in the Warehouse context recording that a specific quantity of a stock item is reserved for an order.

## Wolverine

The messaging framework (Wolverine Fx) used for in-process command dispatch and RabbitMQ message publishing/consuming, with EF Core transactional outbox support. Version: 5.14.0.

## YARP

Yet Another Reverse Proxy — the .NET library used in `Gateway.Api` to route external HTTP requests to internal bounded context services without direct inter-service HTTP calls.

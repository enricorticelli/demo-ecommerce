# ADR-0009: Event-Driven Email via a Dedicated Communication Service

- Status: accepted
- Date: 2026-04-22

## Context

Transactional emails (order confirmations, cancellation notices) must be triggered by domain events without coupling the Order or Payment services to an SMTP library.

## Decision

A dedicated **Communication** bounded context (`src/Communication.*`) listens to integration events (e.g., `OrderCompletedV1`, `OrderCancelledV1`) via RabbitMQ/Wolverine and sends transactional email via SMTP.

- Domain entity: `CommunicationEmailMessage` (`src/Communication.Domain/Entities/CommunicationEmailMessage.cs`).
- SMTP settings injected via `Communication__Smtp__*` environment variables (`.env` lines 52–55).
- Local development uses Mailpit (`axllent/mailpit:v1.26.2`, `docker-compose.yml` line 184) as a mock SMTP server.
- The Communication service is the only service that sends email. Other services never call an SMTP library directly.

## Consequences

- Email delivery is asynchronous and decoupled from the originating transaction.
- Email logic (templates, retry) is contained in one service.
- A failed email does not roll back the order or payment.

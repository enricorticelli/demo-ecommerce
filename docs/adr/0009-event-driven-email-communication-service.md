# ADR-0009: Communication service for event-driven external emails

- Date: 2026-03-08
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

User communications (order confirmation) must be introduced without coupling core bounded contexts to email providers or synchronous inter-service calls.

## Decision

Adopt a new `Communication` bounded context with asynchronous event consumption and SMTP email sending.

1. `Order` publishes `OrderCompletedV1` with complete payload (including `CustomerEmail`).
2. `Communication` consumes the event and sends email through SMTP adapter.
3. The same event can be consumed in parallel by multiple bounded contexts.
4. Consumer idempotency is mandatory with persistent deduplication.
5. Local environment uses Mailpit as SMTP mock and inbox UI.

## Alternatives considered

1. Direct email sending from `Order`: technical coupling and mixed responsibilities.
2. HTTP call from `Communication` to other contexts for email data: temporal coupling and fragility.
3. Real email provider from day one: unnecessary operational complexity in early phase.

## Consequences

### Positive

- Clear separation between core domain and communication channel.
- Better resilience via asynchronous integration and deduplication.
- Easier extension for new templates/channels without touching core services.

### Negative / Trade-offs

- One more service to maintain and monitor.
- Overhead of a dedicated service and queues.

## Implementation impact

- Shared contract `OrderCompletedV1` reused by multiple consumers.
- RabbitMQ queues: `order-completed-communication` (email) and other consumer-specific queues on fanout `order-completed`.
- New configuration keys: `ConnectionStrings__CommunicationDb`, `Communication__Smtp__*`.
- New `communication-api` service and `mailpit` container in `docker-compose`.

## Adoption plan

1. Introduce `Communication` module and infrastructure wiring.
2. Update `Order` producer to publish a single shared event.
3. Add handler/contract tests and local smoke test with Mailpit.

## References

- `../architecture.md`
- `../bounded-contexts/communication.md`
- `../guidelines/integration-events.md`

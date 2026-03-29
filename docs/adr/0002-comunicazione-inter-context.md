# ADR-0002: Communication between bounded contexts

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

Business processes span multiple contexts (for example order, payment, shipping, warehouse). A consistent strategy is needed to choose between synchronous and asynchronous communication, avoiding strong coupling and ambiguous semantics.

## Decision

Adopt a mixed communication model with explicit rules:

1. synchronous HTTP for user requests, immediate queries, and API-boundary commands;
2. asynchronous event-driven communication for cross-context workflows and multi-step state transitions;
3. API gateway only for routing and cross-cutting policies, never for domain orchestration;
4. versioned and backward-compatible integration contracts.

## Alternatives considered

1. HTTP-only synchronous: simple at first, but fragile for resilience and temporal coupling.
2. Event-driven only: decoupled but over-complex for simple use cases and debugging.
3. Centralized orchestration in gateway: violates domain boundaries.

## Consequences

### Positive

- Reduced temporal coupling between contexts.
- Better resilience for distributed workflows.
- Clearer responsibilities and ownership.

### Negative / Trade-offs

- Increased observability and tracing complexity.
- Need explicit policies for retry, idempotency, and ordering.

## Implementation impact

- Define event guidelines in `docs/guidelines/integration-events.md`.
- Add contract tests for cross-context APIs/events.
- Standardize correlation id and structured logging.

## Adoption plan

1. Define application-domain events per context.
2. Apply retry/idempotency policies in handlers.
3. Monitor distributed workflows with metrics and tracing.

## References

- `../architecture.md`
- `./0003-data-ownership-database-separati.md`
- `../guidelines/integration-events.md`

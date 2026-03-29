# ADR-0010: Inter-context communication is event-driven only

- Date: 2026-03-29
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team
- Supersedes: `./0002-inter-context-communication.md`

## Context

The system has multiple bounded contexts with independent ownership and lifecycle. Synchronous HTTP calls between contexts create temporal coupling, increase cascading failures, and complicate autonomous scaling and releases.

The project now requires a strict rule: cross-context communication must be asynchronous and event-driven only.

## Decision

Adopt an event-only communication model for bounded contexts with explicit rules:

1. synchronous HTTP between bounded contexts is forbidden;
2. all cross-context communication must happen through versioned integration events;
3. API gateway remains an edge component for client-to-system traffic and cross-cutting policies, never for domain orchestration;
4. integration contracts must be explicit, versioned, and backward-compatible where possible.

## Alternatives considered

1. Mixed model (HTTP + events): more flexible, but allows temporal coupling and drift from resilience goals.
2. HTTP-only synchronous: simpler initially, but fragile for distributed workflows and failure isolation.
3. Event-driven only: stronger decoupling and resilience, with higher observability and operational discipline requirements.

## Consequences

### Positive

- Strong reduction of temporal coupling across contexts.
- Better resilience and failure isolation.
- Clearer ownership boundaries and independent evolution.

### Negative / Trade-offs

- Higher complexity in observability, replay, and incident analysis.
- Need strict idempotency, retry, and dead-letter policies.
- Eventual consistency must be handled explicitly in business flows.

## Implementation impact

- Remove or prevent synchronous inter-context HTTP dependencies.
- Standardize event publication/consumption contracts in shared contracts.
- Strengthen contract tests and consumer-driven compatibility checks.
- Enforce outbox/inbox patterns and deduplication in all relevant flows.

## Adoption plan

1. Inventory existing inter-context calls and migrate synchronous flows to events.
2. Add missing integration events and versioned contracts.
3. Add/extend tests for idempotency, retries, and compensation.
4. Update architecture and guidelines documentation.

## References

- `../architecture.md`
- `../guidelines/integration-events.md`
- `../guidelines/backend-ddd.md`
- `./0005-eventual-consistency-compensations.md`
- `./0006-idempotency-deduplication.md`

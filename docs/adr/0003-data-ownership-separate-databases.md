# ADR-0003: Data ownership and separate databases per bounded context

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

The solution needs semantic isolation between contexts and must handle medium/high load. Shared databases would increase coupling and make independent domain evolution harder.

## Decision

Each bounded context owns its data and its own database/schema.

1. No direct database access to another context.
2. Data integration only via APIs or events.
3. Independent persistence models per context.
4. Migrations and data lifecycle governed by the owning context.

## Alternatives considered

1. Single shared database: simple initially, high coupling and cross-context regression risk.
2. Single database with separated schemas but mixed access: reduces part of the risk but still violates ownership.
3. Ungoverned data copy: fast short-term, high technical debt mid-term.

## Consequences

### Positive

- Independent context evolution.
- Better alignment with DDD bounded contexts.
- Fewer side effects from data model changes.

### Negative / Trade-offs

- More integration and synchronization work between contexts.
- Need observability and distributed reconciliation.

## Implementation impact

- Define ownership for each business entity in bounded context docs.
- Introduce contract versioning and backward-compatible migration policies.
- Plan compensation mechanisms for temporary inconsistencies.

## Adoption plan

1. Map data ownership per context in `docs/bounded-contexts/`.
2. Implement integrations only through public contracts.
3. Add architecture checks in review to prevent cross-db access.

## References

- `../architecture.md`
- `./0002-inter-context-communication.md`

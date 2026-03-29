# ADR-0004: Contract-first and API/event versioning

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

With separated bounded contexts, contract stability is essential to avoid regressions between services and frontend consumers. Domain evolution is frequent, but integrations must remain compatible.

## Decision

Adopt contract-first governance for APIs and events:

1. every public endpoint/event is defined as a versioned contract;
2. breaking changes require a new version (`v2`, `V2`);
3. non-breaking changes are allowed only when backward-compatible;
4. deprecation requires an explicit, communicated time window.

For contextual HTTP namespace:

1. use `store` for storefront APIs;
2. use `backoffice` for management/full CRUD APIs.

## Alternatives considered

1. Code-first without governance: faster short-term, high breakage risk between contexts.
2. Versioning only HTTP APIs: insufficient for event-driven workflows.
3. Team-by-team ad hoc versioning: inconsistency and growing technical debt.

## Consequences

### Positive

- Predictable contract evolution.
- Better producer/consumer stability.
- Stronger separation between internal model and integration model.

### Negative / Trade-offs

- Higher documentation and review overhead.
- Need contract compatibility testing.

## Implementation impact

- Define DTO/API contracts independent from internal domain model.
- Introduce consistent naming/versioning rules.
- Update docs and changelog for each version.

## Adoption plan

1. Catalog current API/event contracts per context.
2. Define compatibility and deprecation policies.
3. Add contract tests in the build pipeline.

## References

- `./0010-inter-context-communication-events-only.md`
- `./0003-data-ownership-separate-databases.md`
- `../guidelines/integration-events.md`

# ADR-0008: Backend test strategy for distributed architecture

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

Moving from mock endpoints to a real backend requires safe evolution. In distributed architecture, testing must cover domain logic, local integration, and contract compatibility.

## Decision

Adopt a multi-layer test strategy:

1. unit tests on domain and application handlers;
2. integration tests on endpoints and persistence per context;
3. contract tests for cross-context APIs/events;
4. selective end-to-end tests on core workflows.

## Alternatives considered

1. E2E only: expensive, slow, fragile for diagnosis.
2. Unit tests only: insufficient coverage for real integrations.
3. Ad hoc tests without strategy: unmanaged suite growth.

## Consequences

### Positive

- Reduced regressions during service evolution.
- Higher confidence for refactoring and contract changes.
- Fast feedback on domain and integration errors.

### Negative / Trade-offs

- Initial time investment for pipeline and fixtures.
- Ongoing maintenance of contract tests.

## Implementation impact

- Define testing standards for each context.
- Make existing tests buildable and reliable.
- Introduce progressive quality gates in CI.

## Adoption plan

1. Make core context tests green.
2. Add contract tests for shared APIs/events.
3. Enable minimum quality gates in pipeline.

## References

- `./0004-contract-first-versioning.md`
- `./0006-idempotency-deduplication.md`
- `../guidelines/implementation-roadmap.md`

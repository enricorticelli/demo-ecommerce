# ADR-0001: Pragmatic microservices architecture model

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

The current backend exposes mock APIs, but the solution is already organized into separated bounded contexts (`Catalog`, `Cart`, `Order`, `Payment`, `Shipping`, `Warehouse`) with a dedicated gateway. The team is small, time-to-market is critical, and expected load is medium/high.

## Decision

Adopt a pragmatic microservices model:

1. separated bounded contexts as logical deployment units;
2. per-context data ownership;
3. contract-first integration;
4. infrastructure complexity introduced only when needed.

## Alternatives considered

1. Modular monolith: operationally simpler, but reduces isolation and independent evolution for already separated contexts.
2. Full enterprise microservices: maximum autonomy, but excessive overhead for a single team.
3. Ungoverned hybrid architecture: flexible short-term, high risk of architectural inconsistency.

## Consequences

### Positive

- Clear domain boundaries aligned with strategic DDD.
- Per-context scalability and evolution.
- Lower semantic coupling risk.

### Negative / Trade-offs

- Higher operational complexity compared to a monolith.
- Requires high discipline on contracts, observability, and testing.

## Implementation impact

- Each context implements complete vertical slices (API + application + domain + infrastructure).
- No business logic in the gateway.
- Any cross-context architectural change requires an ADR.

## Adoption plan

1. Formalize operational decisions in ADR-0002 and ADR-0003.
2. Implement first real flows in `Order`, `Payment`, `Shipping`.
3. Extend the same governance to other contexts.

## References

- `../architecture.md`
- `./0002-comunicazione-inter-context.md`
- `./0003-data-ownership-database-separati.md`

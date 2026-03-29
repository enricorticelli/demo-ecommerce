# ADR-0005: Eventual consistency and compensation in distributed workflows

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

Order flows involve multiple bounded contexts with separated data ownership. Strong cross-context consistency is not realistic without greatly increasing coupling and operational cost.

## Decision

Adopt eventual consistency across bounded contexts with explicit rules:

1. strong transactions only inside a single context;
2. event-driven cross-context workflows;
3. application-level compensation for error handling;
4. explicit intermediate states for long-running processes.

## Alternatives considered

1. Distributed strong consistency: high technical/operational cost and reduced resilience.
2. Best-effort without compensation: high risk of permanent inconsistent states.
3. Hard-coded centralized orchestration: fragile and hard to evolve.

## Consequences

### Positive

- Better resilience against partial failures.
- More scalability and context autonomy.
- Clearer asynchronous process behavior.

### Negative / Trade-offs

- Temporary inconsistencies will exist.
- Reconciliation and monitoring mechanisms are required.

## Implementation impact

- Model explicit states and transitions for order/payment/shipping.
- Define compensating actions for each critical step.
- Expose process states transparently to API consumers.

## Adoption plan

1. Define state/transition maps for core workflows.
2. Implement minimum compensation on failure paths.
3. Add metrics and alerts for stuck workflows.

## References

- `./0002-comunicazione-inter-context.md`
- `./0003-data-ownership-database-separati.md`
- `../architecture.md`

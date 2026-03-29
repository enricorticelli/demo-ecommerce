# ADR-0006: Handler idempotency and message deduplication

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

In event-driven integration, message delivery can cause retries and duplicates. Without idempotency, consumers may apply the same transition multiple times, introducing domain bugs.

## Decision

Make all asynchronous consumers idempotent and add deduplication:

1. every handler must tolerate duplicate delivery of the same event;
2. use deduplication keys (`eventId`, `correlationId`) for tracking;
3. treat out-of-order delivery as an expected case;
4. define safe replay policies.

## Alternatives considered

1. Rely only on broker semantics: does not remove application-level duplicates.
2. Idempotency only for critical flows: leaves fragile areas.
3. Compensation without idempotency: insufficient and costly.

## Consequences

### Positive

- Reduced side effects from retries/duplicates.
- Higher reliability of distributed workflows.
- Easier recovery and controlled replay.

### Negative / Trade-offs

- Higher consumer complexity.
- Need storage and governance for deduplication.

## Implementation impact

- Define technical standards for idempotency checks in every handler.
- Introduce upsert/compare-and-set logic where needed.
- Add dedicated tests for duplicates and out-of-order events.

## Adoption plan

1. Introduce shared technical guidelines for consumers.
2. Incrementally adapt critical workflows.
3. Verify replay/duplication scenarios in integration tests.

## References

- `./0002-inter-context-communication.md`
- `./0005-eventual-consistency-compensations.md`
- `../guidelines/integration-events.md`

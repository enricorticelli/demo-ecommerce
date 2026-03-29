# Architecture Decision Records (ADR)

## Status

- `Accepted`: active and applicable decision.
- `Proposed`: under evaluation.
- `Superseded`: replaced by a newer ADR.
- `Deprecated`: kept only for historical reference.

## ADR index

| ADR | Title | Status | Date |
| --- | --- | --- | --- |
| [0001](./0001-microservices-pragmatici.md) | Architectural model: pragmatic microservices | Accepted | 2026-03-07 |
| [0002](./0002-comunicazione-inter-context.md) | Communication between bounded contexts | Accepted | 2026-03-07 |
| [0003](./0003-data-ownership-database-separati.md) | Data ownership and separate databases | Accepted | 2026-03-07 |
| [0004](./0004-contract-first-versioning.md) | Contract-first and contract versioning | Accepted | 2026-03-07 |
| [0005](./0005-eventual-consistency-compensazioni.md) | Eventual consistency and compensation | Accepted | 2026-03-07 |
| [0006](./0006-idempotenza-deduplica.md) | Idempotency and message deduplication | Accepted | 2026-03-07 |
| [0007](./0007-osservabilita-distribuita-minima.md) | Minimum distributed observability | Accepted | 2026-03-07 |
| [0008](./0008-strategia-test-backend.md) | Backend test strategy | Accepted | 2026-03-07 |
| [0009](./0009-servizio-comunicazioni-email-event-driven.md) | Communication service for event-driven external emails | Accepted | 2026-03-08 |

## Maintenance rules

1. For a new non-trivial decision, create a new ADR from the template.
2. If a decision changes, do not rewrite history: create a new ADR and mark the previous one as `Superseded`.
3. Every ADR must include context, decision, alternatives, consequences, and trade-offs.
4. Every accepted ADR must be referenced by the relevant documentation.

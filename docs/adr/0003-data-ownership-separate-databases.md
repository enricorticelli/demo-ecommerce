# ADR-0003: Data Ownership — Separate Database per Bounded Context

- Status: accepted
- Date: 2026-04-22

## Context

Sharing a single database across contexts couples their schemas, migration timelines, and deployment cycles. Each bounded context must evolve its schema independently.

## Decision

Each bounded context owns a **dedicated PostgreSQL database** (`[context]_db`). Databases are created on PostgreSQL container startup via `scripts/postgres/init-multiple-databases.sh`. Connection strings are injected via environment variables following the pattern `ConnectionStrings__[Context]Db` (`.env` lines 32–39).

No bounded context may query another context's database directly. Cross-context data needs are fulfilled by integration events (ADR-0002) or by the consuming context maintaining a local denormalised copy.

## Consequences

- Schema migrations in one context cannot break another.
- No cross-database foreign keys or joins.
- Data that multiple contexts need (e.g., product info in cart) must be replicated via events.

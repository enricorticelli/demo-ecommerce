# Architecture Documentation

This folder contains architectural decisions, guidelines, and reference documents for the e-commerce backend.

## Quick index

- `ai.md`: general development standards.
- `architecture.md`: target architecture and boundaries between bounded contexts.
- `adr/README.md`: Architecture Decision Records index.
- `bounded-contexts/`: responsibilities and boundaries for each context.
- `guidelines/`: operational guidelines for incremental implementation.
- `guidelines/module-baseline-conventions.md`: reusable technical baseline for all backend modules.
- `guidelines/endpoint-conventions.md`: HTTP endpoint conventions (paths, contexts, versioning, whitelist).

Adopted endpoint contexts:

- `store`: storefront APIs (`/api/store/{service}/v1/...`)
- `backoffice`: management APIs (`/api/backoffice/{service}/v1/...`)

## Guiding principles

1. Domain first, technology second.
2. Pragmatic microservices: semantic isolation with controlled operational complexity.
3. Contract-first: versioned and backward-compatible APIs and events.
4. Explicit eventual consistency with compensation and idempotency.
5. Incremental evolution by vertical slices, avoiding big-bang changes.

## How to use this documentation

1. Read `architecture.md` for the big picture.
2. Check ADRs in `adr/` before introducing new technical decisions.
3. Follow guidelines in `guidelines/` during implementation.
4. Update or add an ADR when an architectural decision changes.

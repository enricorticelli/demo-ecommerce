# ADR 0001: Use Wolverine + Marten

## Status
Accepted

## Context
The system requires CQRS, event sourcing, and reliable asynchronous messaging in a docker-first setup.

## Decision
Use Wolverine for command/event handling and durable messaging, and Marten for event sourcing and document persistence on PostgreSQL.

## Consequences
- Fast developer loop with .NET-native stack
- Single persistence engine for streams + documents
- Requires familiarity with Wolverine conventions

# ADR-0010: No Synchronous HTTP Between Bounded Contexts

- Status: accepted
- Date: 2026-04-22

## Context

ADR-0002 chose asynchronous events for inter-context communication. This ADR makes the constraint explicit and testable: bounded contexts must not call each other's HTTP APIs at runtime.

## Decision

Direct HTTP calls between bounded contexts are **forbidden**. Any information a context needs from another must arrive via:
1. An integration event consumed from RabbitMQ, or
2. Data the consuming context has already stored locally from a prior event.

Architecture tests (`CommonArchitectureTests.ApiEndpoints_WhenInspected_DoNotUseInfrastructureOrDirectEventPublishing`, line 138) verify that endpoint files do not reference cross-context infrastructure.

The API Gateway (`src/Gateway.Api/`) is the only component that aggregates HTTP routes from multiple services — it does so as a reverse proxy (YARP), not by calling service internals.

## Consequences

- No runtime HTTP dependency between bounded contexts.
- Eventual consistency is the only consistency model between contexts.
- The Gateway OpenAPI composer may read multiple OpenAPI specs, but only for documentation aggregation, not for data.

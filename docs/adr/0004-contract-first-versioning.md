# ADR-0004: Contract-First Versioning of Integration Events

- Status: accepted
- Date: 2026-04-22

## Context

Integration events cross service boundaries and are consumed by independent deployments. Unversioned events make backwards-incompatible changes dangerous and hard to coordinate.

## Decision

All integration events are defined as C# records in `src/Shared.BuildingBlocks/Contracts/IntegrationEvents/` and carry an explicit version suffix in their type name (e.g., `ProductCreatedV1`, `OrderCompletedV1`).

- `IntegrationEventBase` is the base record (`Shared.BuildingBlocks/Contracts/IntegrationEvents/IntegrationEventBase.cs`).
- `IntegrationEventMetadata` carries correlation and causation identifiers.
- A new incompatible shape requires a new version record (e.g., `ProductCreatedV2`); both versions coexist until consumers are migrated.
- Existing event records must not change field names or types.

## Consequences

- Consumers can be deployed independently of producers.
- Breaking changes are always additive (new version record), never in-place.
- The `Shared.BuildingBlocks` project becomes a shared contract library — keep it free of domain business logic.

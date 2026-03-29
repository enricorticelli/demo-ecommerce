# Tactical Backend DDD Guidelines

These guidelines translate ADR decisions into implementation rules for backend code.

## Goal

Implement real business logic in `Application` and `Domain`, while keeping `Api` as a pure adapter layer.

## Modeling rules

1. `Entity`: use when identity and lifecycle exist.
2. `Value Object`: use for immutable concepts with value equality.
3. `Aggregate Root`: one root per transactional boundary.
4. `Domain Service`: only for rules that do not clearly belong to one aggregate.
5. `Repository`: interface in `Application` or `Domain`, implementation in `Infrastructure`.

## Application rules

1. Endpoints must not contain business logic.
2. Endpoints depend on application command/query services, not technical details.
3. Every command changes state in one bounded context only.
4. No direct dependency between `Domain` layers of different contexts.
5. Separate `CommandService` and `QueryService` responsibilities.
6. Move cross-entity rules to `Rules/Policy/Specification`, not endpoints.
7. Keep `Entity -> View` mapping in `Application` and `View -> Response` mapping in static API mappers.

## Infrastructure rules

1. Keep `Program.cs` minimal: only module extension calls (`Add...Module`, `Use...ModuleAsync`).
2. Repository implementations in `Infrastructure`, interfaces in `Application`.
3. Publish events through `IDomainEventPublisher`; technology adapters stay in `Infrastructure`.
4. Implement search/listing in dedicated query components (query object/specification), not fat services.
5. One type per file, avoid container classes.

## Cross-context flow rules

1. Integrate between bounded contexts only through integration event contracts (no synchronous HTTP between modules).
2. Always enforce idempotency in asynchronous consumers.
3. Define compensation for multi-step failures.

## Technical Definition of Done

1. Domain invariants are tested.
2. Application handlers are tested.
3. Endpoints have minimal integration tests.
4. No mock/stub behavior on released critical paths.

## Test naming convention

Always use `Method_Scenario_ExpectedBehavior` for test names.

Examples:

1. `GetBooks_EmptyDatabase_ReturnsEmptyList`
2. `Add_TwoPositiveNumbers_ReturnsSum`

## Related ADRs

- `../adr/0001-pragmatic-microservices.md`
- `../adr/0010-inter-context-communication-events-only.md`
- `../adr/0003-data-ownership-separate-databases.md`
- `../adr/0005-eventual-consistency-compensations.md`
- `../adr/0006-idempotency-deduplication.md`
- `../adr/0008-backend-test-strategy.md`
- `./module-baseline-conventions.md`

# Module Baseline Conventions

## Goal

Define a reusable and uniform baseline for all backend bounded contexts.

## Layer structure

1. `Api`
2. `Application`
3. `Domain`
4. `Infrastructure`
5. `Shared.BuildingBlocks` (cross-context components only)

## API conventions

1. Keep `Program.cs` minimal: only module extension calls (`Add<Context>Module`, `Use<Context>ModuleAsync`).
2. Endpoints contain no business logic: validate basic input, read correlation id, delegate to application services.
3. Keep `View -> Response` mapping in dedicated static mappers, one mapper per response type.
4. `Api` must not depend on technical details (EF, broker, Wolverine, SQL).
5. Endpoint exposure must follow `guidelines/endpoint-conventions.md`:
   - `store` for storefront/user journey
   - `backoffice` for management APIs (full CRUD)

## Application conventions

1. Clear command/query separation.
2. Use `*CommandService` for write use cases.
3. Use `*QueryService` for listing, details, and search (`searchTerm`).
4. Repositories are persistence abstractions (`I*Repository`) without HTTP concerns or response mapping.
5. Put cross-entity rules in policy/specification (`I*Rules`), not in endpoints.
6. Keep `Entity -> View` mapping in `Application` via shared `IViewMapper<TEntity, TView>`.
7. Avoid fat services: one service per responsibility, composed via dependency inversion.

## Infrastructure conventions

1. Keep EF Core repository implementations separate from application services.
2. Isolate search logic in dedicated query objects/components.
3. Publish events through infrastructure adapters (`OutboxDomainEventPublisher`) behind application abstraction (`IDomainEventPublisher`).
4. Handle outbox/inbox and durability with Wolverine, without leaking details into `Application`.
5. Centralize DI registrations in module infrastructure extensions.

## Shared conventions

1. Integration events live in `Shared.BuildingBlocks.Contracts.IntegrationEvents.<Context>`.
2. Use versioned event names: `<EventName>V1`.
3. One type per file.
4. Required metadata: `eventId`, `occurredAtUtc`, `correlationId`, `sourceContext`.
5. Reusable abstractions stay in shared (for example `IDomainEventPublisher`, `IViewMapper<,>`, standard application exceptions).

## Code conventions

1. One type per file.
2. Avoid container classes without clear responsibility.
3. Use explicit, domain-oriented names.
4. Never access another bounded context database directly.

## Minimum testing per module

1. Unit tests for rules/policies.
2. Unit tests for command/query services with mock/fake dependencies.
3. EF repository integration tests on PostgreSQL.
4. Event contract tests (name/version/metadata).
5. Endpoint tests to confirm stable HTTP contracts.

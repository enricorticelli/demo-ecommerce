# G-DDD: Backend Domain-Driven Design Conventions

- Status: active

## Aggregates and Entities

- Domain entities are plain C# classes with private setters. State changes happen only via named methods (e.g., `Order.MarkCompleted()`, `Order.ApplyPaymentAuthorized()`). Evidence: `src/Order.Domain/Entities/Order.cs`.
- Factory methods (`Create(…)`) validate invariants and throw typed domain exceptions (`ValidationAppException`, `ConflictAppException`) rather than returning nulls.
- Constructors are private; EF Core uses the parameterless private constructor for hydration.

## Value Objects

- Represented as C# records (immutable by default). Examples: `OrderAddress`, `OrderCustomer`, `OrderItem` in `src/Order.Domain/ValueObjects/`.
- Value objects carry no identity; equality is structural.

## Enumerations

- Domain state machines use C# enums (e.g., `OrderStatus` in `src/Order.Domain/Entities/OrderStatus.cs`).
- Transitions are validated inside the aggregate; invalid transitions throw `ConflictAppException`.

## Repositories

- Interfaces live in `[Context].Application/Abstractions/Repositories/` and follow the naming `I[Entity]Repository`.
- Implementations live in `[Context].Infrastructure/Persistence/Repositories/` and follow the naming `[Entity]Repository`.
- Enforced by `CommonArchitectureTests.RepositoryConventions_WhenValidated_AreConsistent()`.

## Command and Query Services

- Application layer exposes `I[Context]CommandService` and `I[Context]QueryService` interfaces.
- Command services mutate state; query services read state. They are never mixed.

## Domain Event Publishing

- Use `IDomainEventPublisher` (from `Shared.BuildingBlocks`) inside application services. Never publish from endpoints or domain entities.
- The infrastructure implementation (`OutboxDomainEventPublisher`) wraps Wolverine's `IDbContextOutbox` for transactional guarantees.

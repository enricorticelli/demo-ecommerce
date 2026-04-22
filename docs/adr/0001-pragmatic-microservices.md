# ADR-0001: Pragmatic Microservices via Bounded Contexts

- Status: accepted
- Date: 2026-04-22

## Context

The system must separate business domains (Catalog, Cart, Order, Payment, Warehouse, Shipping, Communication) to allow independent deployability and clear ownership. A strict shared-database monolith would couple all domains; a full microservices mesh would add operational overhead.

## Decision

Organise the backend as **autonomous bounded contexts**, each deployed as its own ASP.NET Core service. Every context owns its domain layer, application layer, infrastructure layer, and API layer. Architecture tests (`src/Architecture.Tests.Common/CommonArchitectureTests.cs`) enforce the layer contract automatically.

Recognised contexts: Catalog, Cart, Order, Payment, Warehouse, Shipping, Communication.

Each service follows the four-layer project structure:
- `[Context].Domain` – entities, value objects, domain rules. No framework dependencies.
- `[Context].Application` – use cases, command/query services, repository interfaces, message handlers.
- `[Context].Infrastructure` – EF Core DbContext, repository implementations, Wolverine wiring.
- `[Context].Api` – ASP.NET Core minimal API endpoints.

## Consequences

- A shared `Shared.BuildingBlocks` project (`src/Shared.BuildingBlocks/`) carries cross-cutting concerns (observability, API defaults, integration event contracts). It must stay thin.
- Architecture tests prevent cross-context assembly references and wrong dependency directions.
- Each context is deployable and scalable independently.

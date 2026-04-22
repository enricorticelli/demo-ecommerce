# ADR-0008: Backend Test Strategy — Unit, Integration, and Architecture Tests

- Status: accepted
- Date: 2026-04-22

## Context

Test coverage must catch domain logic bugs, handler correctness, and architectural drift without requiring a running infrastructure stack for every test run.

## Decision

Each bounded context has a `[Context].Tests` project that mixes three test kinds:

**Unit tests** (xUnit 2.9.3 + Moq 4.20.72):
- Domain entity behaviour (e.g., `OrderDomainTests.cs`)
- Application command/query services with mocked repositories (e.g., `OrderCommandServiceTests.cs`)
- Mapper security checks (e.g., `OrderMapperSecurityTests.cs`)

**Integration tests**:
- EF Core persistence using the `Microsoft.EntityFrameworkCore.InMemory` provider (e.g., `OrderDbContextModelTests.cs`)
- Message handler tests with mocked publishers (e.g., `OrderIntegrationHandlersTests.cs`)

**Architecture tests** (NetArchTest.Rules 1.3.2 via `src/Architecture.Tests.Common/CommonArchitectureTests.cs`):
- All four layer projects exist.
- Layer dependency direction is enforced at the assembly level.
- Domain assembly has no dependency on EF Core, Wolverine, Npgsql, or ASP.NET Core.
- No cross-context assembly references.
- Endpoint files do not import infrastructure directly.
- `Program.cs` does not contain low-level wiring (line 161–175).
- Repository naming conventions (`I*Repository` / `*Repository`) are verified.

## Consequences

- Breaking a layer boundary fails CI before review.
- The shared `Architecture.Tests.Common` assembly is linked into every context test project, ensuring uniform enforcement.

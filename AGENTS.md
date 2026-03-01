# CODING STANDARDS — .NET Minimal API Microservices

> Purpose: define standards and principles for producing readable, testable, maintainable, and secure code.
> Applies to humans and AI agents working in this repository.

---

## 1) North Star: quality over speed

### Non-negotiable goals
- **Correctness**: code must work and handle edge cases.
- **Readability**: code is read more often than written.
- **Maintainability**: easy changes, hard regressions.
- **Testability**: design that supports automated testing.
- **Security**: no secrets in the repo; validate all inputs.
- **Consistency**: uniform conventions across the project.

### Golden rule
> If it's not clear *what* it does and *why* it does it, it's not done.

---

## 2) Clean Code principles

### Naming
- Names must be **explicit**, **pronounceable**, **searchable**.
- Follow .NET naming conventions:
  - `PascalCase` for classes, methods, properties, interfaces, enums.
  - `camelCase` for local variables and parameters.
  - `_camelCase` for private fields.
  - Prefix interfaces with `I` (`IOrderRepository`, `IPaymentService`).
- Functions: verb + object (`CalculateTotal`, `FetchUserById`).
- Classes/modules: nouns (`OrderService`, `InvoiceRepository`).
- Booleans: `is/has/can/should` prefixes (`isValid`, `hasPermission`).

### Small functions, single purpose
- A function should do **one thing**.
- Keep endpoint handlers thin — delegate to use-case / service classes.
- Use extension methods to organize endpoint registration (`MapOrderEndpoints()`).
- Avoid boolean flags that change behavior (code smell).

### Comments
- Prefer **self-explanatory code** over comments.
- Comment the **why**, not the **what**.
- XML doc (`///`) on public types and members that form an API contract.

### Structure & complexity
- Reduce nesting (early returns, guard clauses).
- Avoid duplication (DRY) but don't abstract too early.
- Watch cyclomatic complexity; refactor when it grows.

---

## 3) SOLID (practical guidance)

### S — Single Responsibility
- One class changes for **one** reason.
- Separate endpoint definition, request validation, domain logic, persistence.

### O — Open/Closed
- Extend behavior without modifying existing code:
  - strategies, polymorphism, composition.
  - Use `IFeatureManager` or feature flags for controlled rollouts.

### L — Liskov Substitution
- Subtypes must honor contracts — no surprises in pre/post-conditions.

### I — Interface Segregation
- Small interfaces tailored to clients.
- Avoid "god interfaces": prefer `IOrderReader` + `IOrderWriter` over one giant `IOrderRepository`.

### D — Dependency Inversion
- Depend on abstractions via the built-in DI container (`IServiceCollection`).
- Register services in dedicated extension methods (`AddOrderModule()`).

---

## 4) Architecture: separation of concerns

### Layering per microservice

```
src/
├── ServiceName.Api/          # Minimal API — endpoint definitions, middleware, DI wiring
├── ServiceName.Application/  # Use cases, CQRS handlers, DTOs, interfaces
├── ServiceName.Domain/       # Entities, value objects, domain events, invariants
└── ServiceName.Infrastructure/ # EF Core, HTTP clients, message brokers, secret access
```

### Practical rules
- **Domain** must not reference EF Core, ASP.NET, or any IO library.
- **Application** orchestrates; no infrastructure details.
- **Infrastructure** implements application interfaces.
- **API** is a thin adapter: map HTTP → use-case → HTTP response.
- Use **MediatR** (or equivalent) to decouple endpoint handlers from use-case logic.
- Use **FluentValidation** for input validation; register validators from Application layer.

---

## 5) Minimal API conventions

- Register endpoints in feature-specific extension methods:
  ```csharp
  app.MapGroup("/orders").MapOrderEndpoints();
  ```
- Return typed results with `TypedResults` (not `Results.Ok(obj)` alone):
  ```csharp
  static async Task<Results<Ok<OrderDto>, NotFound>> GetOrder(
      int id, ISender mediator) { ... }
  ```
- Annotate endpoints with `.WithName()`, `.WithTags()`, `.WithOpenApi()` for Swagger/OpenAPI.
- Use `IEndpointFilter` for cross-cutting concerns (validation, auth checks).
- Define route constants to avoid magic strings.

---

## 6) Design patterns

> Patterns are shared vocabulary. Don't use them "just to use patterns".

### Recommended patterns for this stack
- **CQRS (MediatR)**: separate reads and writes; `IRequest<T>` + `IRequestHandler<,>`.
- **Repository + Unit of Work**: abstract EF Core access; don't leak `DbContext` into Application.
- **Outbox Pattern**: reliable domain event publishing to the message broker.
- **Adapter**: wrap external HTTP/gRPC services behind domain interfaces.
- **Decorator**: cross-cutting concerns (logging, caching, retry) via MediatR pipeline behaviors.
- **Factory**: complex entity creation that must satisfy invariants.
- **Strategy**: interchangeable algorithms (pricing, discount rules, etc.).

### Anti-patterns to avoid
- Fat endpoints that contain business logic
- `DbContext` injected directly into endpoint handlers
- Shared databases between microservices (prefer API or async messaging)
- God services with dozens of injected dependencies
- Singleton as global mutable state

---

## 7) Error handling, contracts, and invariants

- Use the **Result pattern** or typed exceptions for domain errors (avoid raw exceptions for control flow).
- Map domain errors to HTTP status codes in a central place (exception middleware or `IProblemDetailsService`).
- Return **RFC 7807 Problem Details** for all error responses:
  ```json
  { "type": "...", "title": "...", "status": 400, "detail": "..." }
  ```
- Validate inputs at the API boundary with FluentValidation; reject early.
- Domain invariants enforced in constructors/factories — never allow invalid objects.
- Never catch-all silently: log and rethrow or convert to typed error.

---

## 8) Logging, observability, and diagnostics

- Use **Serilog** (or Microsoft.Extensions.Logging abstraction) with structured output.
- Always include correlation context:
  ```csharp
  Log.ForContext("CorrelationId", correlationId)
     .ForContext("UserId", userId)
     .Information("Order {OrderId} created", orderId);
  ```
- Levels: `Debug` (dev only) / `Information` / `Warning` / `Error` / `Critical`.
- Never log secrets, tokens, passwords, PII.
- Expose `/health` (liveness + readiness) via `Microsoft.Extensions.Diagnostics.HealthChecks`.
- Instrument with **OpenTelemetry** (traces + metrics) for distributed tracing across services.
- Add metrics for critical paths: request latency, error rate, queue depth.

---

## 9) Testing: pyramid, coverage, reliability

### Priorities
- **Unit tests**: use cases, domain logic, validators — fast, no IO, xUnit + FluentAssertions.
- **Integration tests**: real DB (Testcontainers) or `WebApplicationFactory` for full API pipeline.
- **Contract tests**: Pact or similar to verify inter-service API compatibility.
- **E2E tests**: few, focused on critical business flows.

### Rules
- Every bug fix adds a test that fails before and passes after.
- Tests must be independent (no implicit ordering, no shared state).
- Use `WebApplicationFactory<Program>` for integration tests; replace services with fakes.
- Use **Testcontainers** for PostgreSQL, Redis, message brokers — avoid H2/InMemory in integration tests.
- Avoid flakiness: control time (`TimeProvider`), randomness, network.

### Minimum Definition of Done
- New features: unit + integration tests + essential docs.
- Refactors: existing tests stay green; add tests if behavior changes.

---

## 10) Security & secrets management

- No credentials in code, `appsettings.json`, or logs.
- Use **environment variables** or a secret manager (Azure Key Vault, AWS Secrets Manager, Vault).
- `appsettings.Development.json` for local dev only — gitignored for sensitive values.
- Sanitize inputs; prevent SQL injection (use parameterized EF Core queries — never raw string interpolation).
- Apply least privilege for service identities and DB roles.
- Enable HTTPS; use HSTS in production.
- Validate JWT claims and scopes per endpoint:
  ```csharp
  .RequireAuthorization("orders:write")
  ```

---

## 11) Performance & scalability

- Measure first, then optimize (use BenchmarkDotNet, profiling, APM traces).
- Use `async/await` correctly — avoid `.Result` / `.Wait()` to prevent deadlocks.
- Avoid N+1 queries: use `.Include()` consciously or projections with `Select`.
- Use `IAsyncEnumerable` for streaming large result sets.
- Cache only with a clear, testable invalidation strategy (distributed cache via `IDistributedCache`).
- Timeouts and exponential-backoff retries for external calls — use **Polly** (or `Microsoft.Extensions.Http.Resilience`).
- Prefer `CancellationToken` propagation through the full call chain.

---

## 12) Dependencies, tooling, and automated quality gates

- **Formatter**: `dotnet format` (enforced in CI).
- **Linter / Analyzer**: Roslyn Analyzers + StyleCop or Roslynator; treat warnings as errors in CI.
- **Pre-commit**: Husky.Net or equivalent for format + lint checks.
- **CI pipeline** must fail on:
  - Build errors or meaningful warnings
  - Test failures
  - Format violations
  - Known CVEs (use `dotnet list package --vulnerable`)
- Keep NuGet dependencies updated; review changelogs before upgrading.

---

## 13) Git, PRs, and code review

### Commits
- **Conventional Commits**: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`.
- One commit = one coherent change.

### Pull Requests
- Describe: problem, approach, trade-offs, how to test.
- Include request/response examples or Swagger screenshots when helpful.
- Avoid mega-PRs: split feature work into vertical slices.

### Review checklist
- Readability and naming
- Endpoint contracts and Problem Details consistency
- Complexity and duplication
- Adequate tests (unit + integration)
- Error handling and edge cases
- Security (auth, input validation, no secrets)
- DB migrations (EF Core): reversible, backward-compatible
- OpenAPI spec updated
- Documentation updates

---

## 14) Guidelines for AI agents modifying this repo

### Working mode
- Prefer **minimal, targeted changes**.
- Match existing style and conventions (naming, DI registration pattern, endpoint style).
- Don't add new NuGet packages without strong justification.
- Avoid cosmetic refactors unless requested.

### Before changing code
- Identify the correct extension point (SOLID boundaries, correct layer).
- Find related tests; add missing ones.
- Respect architectural boundaries (Domain must not reference Infrastructure).

### While implementing
- Leave the code better than you found it.
- Update/add tests.
- Update OpenAPI annotations and docs if public behavior changes.

---

## 15) Project-specific conventions

| Item | Value |
|---|---|
| Language | C# 12 / .NET 8+ |
| Formatter | `dotnet format` |
| Linter | Roslyn Analyzers + Roslynator |
| Test runner | xUnit + FluentAssertions + Testcontainers |
| Folder structure | See §4 layering |
| Dependency policy | NuGet, pinned major versions, `--vulnerable` check in CI |
| Logging standard | Serilog, structured, with CorrelationId |
| Error standard | RFC 7807 Problem Details via `IProblemDetailsService` |
| API versioning | URL segment (`/v1/`) or header — consistent across services |
| Async messaging | (fill in: RabbitMQ / Azure Service Bus / Kafka) |
| Service discovery | (fill in: Kubernetes DNS / Consul / Azure APIM) |

---

## 16) Decisions and ADRs
- Document significant architectural decisions in `docs/adr/`.
- Each ADR includes: context, decision, alternatives, consequences.
- Typical candidates: choice of message broker, auth strategy, data-per-service boundaries, caching approach.

---

## 17) Final quality bar
> Code is ready when:
- Build passes with no meaningful warnings
- Relevant unit and integration tests are green
- `dotnet format` reports no violations
- Roslyn analyzer warnings are resolved
- Edge cases are handled and domain invariants are enforced
- No secrets are exposed
- OpenAPI spec reflects the changes
- README/changelog updated if needed
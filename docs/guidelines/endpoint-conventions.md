# G-ENDPOINT: API Endpoint Conventions

- Status: active

## Minimal API Structure

- Endpoints are registered as static extension methods on `IEndpointRouteBuilder` inside `[Context].Api/Endpoints/`.
- Each endpoint file covers one resource (e.g., `OrderEndpoints.cs`).
- `Program.cs` must not contain low-level wiring (`AddDbContext`, `UseNpgsql`, `UseWolverine`, connection strings). Enforced by `CommonArchitectureTests.ProgramBootstrap_WhenInspected_DoesNotContainLowLevelTechnicalWiring()`.

## URL Conventions

- Backoffice routes: `/api/backoffice/[context]/v1/[resource]`
- Storefront routes: `/api/storefront/[context]/v1/[resource]`
- Observed in seeding script: `scripts/seeding/seed-catalog.js` lines 297–300.

## Shared Pipeline

- All services call `builder.AddDefaultApiServices()` and `app.UseDefaultApiPipeline()` from `Shared.BuildingBlocks.Api.DefaultApiExtensions`.
- This registers: SwaggerGen (OpenAPI v1), ProblemDetails, HealthChecks, CORS (`default` policy — all origins).
- Health endpoints: `GET /health/live` and `GET /health/ready` (mapped at lines 47–48 of `DefaultApiExtensions.cs`).
- OpenAPI spec served at `/openapi/{documentName}.json`.

## Endpoint Isolation

- Endpoint files must not import infrastructure namespaces, `EntityFrameworkCore`, `Wolverine`, `Npgsql`, or `IDomainEventPublisher`.
- Enforced by `CommonArchitectureTests.ApiEndpoints_WhenInspected_DoNotUseInfrastructureOrDirectEventPublishing()`.

## Pagination

- Use `PaginationNormalizer.Normalize()` for all paginated list endpoints to ensure consistent page/size defaults.

## Error Responses

- Use `ExceptionHttpResultMapper` to translate domain exceptions to HTTP status codes consistently.
- Do not expose stack traces or internal infrastructure details in error bodies.

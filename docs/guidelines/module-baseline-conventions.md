# G-MODULE: Module Baseline Conventions

- Status: active

## Project Layout

Every bounded context must have exactly four projects plus one test project:

```
src/
  [Context].Api/
  [Context].Application/
  [Context].Domain/
  [Context].Infrastructure/
  [Context].Tests/
```

Enforced by `CommonArchitectureTests.Module_WhenValidated_HasAllLayerProjects()`.

## Project Reference Rules

| Layer          | May reference                                                |
|----------------|--------------------------------------------------------------|
| Domain         | Shared.BuildingBlocks only                                   |
| Application    | Domain, Shared.BuildingBlocks only                           |
| Infrastructure | Application, Domain, Shared.BuildingBlocks only              |
| Api            | Application, Infrastructure, Shared.BuildingBlocks only      |

Cross-context project references are always forbidden. Enforced by `CommonArchitectureTests.LayerProjectReferences_WhenValidated_FollowCommonRules()`.

## Wolverine Wiring

- All Wolverine configuration (RabbitMQ queues, exchanges, PostgreSQL persistence) lives in `[Context].Infrastructure/Configuration/[Context]HostBuilderExtensions.cs`.
- Never place `UseWolverine`, `ListenToRabbitQueue`, or `PersistMessagesWithPostgresql` in `Program.cs`.

## DbContext

- One `DbContext` per bounded context, named `[Context]DbContext`, placed in `[Context].Infrastructure/Persistence/`.
- EF Core migrations live in `[Context].Infrastructure/Persistence/Migrations/`.

## Package Management

- Package versions are centralised in `Directory.Packages.props` at the repository root.
- Add new packages only there; individual `.csproj` files reference packages without version numbers.

## Health and Observability

- Every service must call `builder.AddDefaultApiServices()` and `app.UseDefaultApiPipeline()` from `Shared.BuildingBlocks`.
- Every service must set `OTEL_SERVICE_NAME` in its docker-compose entry.

## Dockerfile Pattern

- Multi-stage build: `mcr.microsoft.com/dotnet/sdk:10.0` for build, `mcr.microsoft.com/dotnet/aspnet:10.0` for runtime.
- Published with `UseAppHost=false` and TFM `net10.0`.

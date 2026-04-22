# ADR-0007: Minimum Distributed Observability via OpenTelemetry and Aspire Dashboard

- Status: accepted
- Date: 2026-04-22

## Context

Debugging distributed systems requires correlated traces, metrics, and logs across services. Instrumenting each service individually with vendor-specific SDKs creates lock-in.

## Decision

All services export telemetry using the **OpenTelemetry SDK** (packages in `Directory.Packages.props` lines 24–28) to a local **Aspire Dashboard** (image `mcr.microsoft.com/dotnet/aspire-dashboard:13`, `docker-compose.yml` line 54).

- Each service sets `OTEL_SERVICE_NAME` and exports to `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889`.
- Instrumented surfaces: ASP.NET Core, HttpClient, EF Core / Npgsql, runtime metrics.
- Evoluzione's in-house package `Evoluzione.TracedServiceCollection` (v10.0.0) standardises the registration boilerplate.
- The `AddObservability()` extension is called from `DefaultApiExtensions.AddDefaultApiServices()` (`src/Shared.BuildingBlocks/Api/DefaultApiExtensions.cs` line 13).
- Correlation IDs are propagated via W3C trace context.

## Consequences

- No vendor lock-in; switching exporters requires only endpoint configuration.
- The Aspire Dashboard is for development. Production requires a real OTLP backend (Jaeger, Tempo, etc.).

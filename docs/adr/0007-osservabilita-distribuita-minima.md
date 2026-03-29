# ADR-0007: Mandatory minimum distributed observability

- Date: 2026-03-07
- Status: Accepted
- Decision Makers: Product/Tech Owner
- Consulted: Project stakeholders
- Informed: Backend/frontend team

## Context

In a distributed architecture, debugging without observability is expensive and slow. A single team needs a solid minimum baseline without introducing overly complex platforms too early.

## Decision

Adopt a mandatory minimum observability standard:

1. structured logging with `correlationId` in all services;
2. baseline metrics for throughput, errors, endpoint and handler latency;
3. distributed tracing for core workflows;
4. `live` and `ready` health checks in every service.

## Alternatives considered

1. Text logs only: insufficient for cross-context tracing.
2. Full enterprise observability from day one: disproportionate overhead.
3. No shared standard: unpredictable incident management.

## Consequences

### Positive

- Lower MTTR for application incidents.
- Better visibility into bottlenecks.
- Better long-term operational reliability.

### Negative / Trade-offs

- Initial instrumentation and dashboard effort.
- Need governance for log/metric data.

## Implementation impact

- Define shared middleware for correlation id.
- Standardize metric names and dimensions.
- Build minimum dashboards for order flow.

## Adoption plan

1. Introduce structured logging in all contexts.
2. Add metrics and tracing in the order flow.
3. Extend progressively to other workflows.

## References

- `./0002-comunicazione-inter-context.md`
- `./0005-eventual-consistency-compensazioni.md`
- `../architecture.md`

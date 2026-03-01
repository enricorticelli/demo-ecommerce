# ADR 0002: Use RabbitMQ as message broker

## Status
Accepted

## Context
The architecture needs async communication among services with strong OSS support and low local setup friction.

## Decision
Use RabbitMQ with management plugin in Docker Compose.

## Consequences
- Mature broker, easy local observability
- Clean integration with Wolverine
- Requires queue topology governance as services evolve

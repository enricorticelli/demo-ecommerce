# Architecture Overview

This repository implements a CQRS and event-sourcing based e-commerce demo using .NET Minimal APIs, Wolverine, Marten, RabbitMQ, and PostgreSQL.

## Core principles
- Write/read separation (CQRS)
- Event-driven workflow orchestration
- Event-sourced aggregates for Cart and Order
- Data-per-service using dedicated PostgreSQL schemas
- Docker-first local environment

## Services
- `Catalog.Api`: product read endpoints and seed
- `Cart.Api`: event-sourced cart aggregate
- `Order.Api`: event-sourced order aggregate + saga orchestration
- `Warehouse.Api`: stock reservation and stock event handling
- `Payment.Api`: payment authorization simulation
- `Shipping.Api`: shipment creation and tracking generation
- `User.Api`: demo user read endpoints and seed
- `Gateway.Api`: YARP reverse-proxy for frontend and clients

## Messaging
RabbitMQ carries integration events. Wolverine provides durable messaging with PostgreSQL-backed inbox/outbox persistence.

## Event flow
1. `OrderPlacedV1`
2. `StockReservedV1` or `StockRejectedV1`
3. `PaymentAuthorizeRequestedV1`
4. `PaymentAuthorizedV1` or `PaymentFailedV1`
5. `ShippingCreateRequestedV1`
6. `ShippingCreatedV1`
7. `OrderCompletedV1` or `OrderFailedV1`
